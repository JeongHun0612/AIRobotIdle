using System;
using System.Collections.Generic;
using System.IO;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    public class GachaService : IGachaService
    {
        private const string SaveFileName = "gacha.json";

        private readonly IResourceService _resourceService;
        private readonly ICurrencyService _currencyService;
        private readonly IEquipmentService _equipmentService;
        private readonly IEventBus _eventBus;

        private GachaLevelConfig _levelConfig;

        // 타입별 핸들러 관리
        private readonly Dictionary<GachaType, IGachaHandler> _handlers = new();

        // 타입별 누적 뽑기 개수 관리
        private readonly Dictionary<GachaType, int> _totalCounts = new();

        public bool IsInitialized { get; private set; }
        public GachaLevelConfig LevelConfig => _levelConfig;

        public GachaService(
            IResourceService resourceService,
            ICurrencyService currencyService,
            IEquipmentService equipmentService,
            IEventBus eventBus)
        {
            _resourceService = resourceService;
            _currencyService = currencyService;
            _equipmentService = equipmentService;
            _eventBus = eventBus;
        }

        public async UniTask InitializeAsync()
        {
            var gachaEquipmentTable = await _resourceService.LoadTableAsync<GachaEquipmentTable>("GachaEquipmentTable");
            var gachaDroneTable = await _resourceService.LoadTableAsync<GachaDroneTable>("GachaDroneTable");
            var equipmentTable = await _resourceService.LoadTableAsync<EquipmentTable>("EquipmentTable");
            var droneTable = await _resourceService.LoadTableAsync<DroneTable>("DroneTable");
            _levelConfig = await _resourceService.LoadAssetAsync<GachaLevelConfig>("GachaLevelConfig");

            if (gachaEquipmentTable == null || gachaDroneTable == null)
            {
                Debug.LogError("[GachaService] 가챠 테이블 로드 실패");
                return;
            }

            if (_levelConfig == null)
            {
                Debug.LogError("[GachaService] GachaLevelConfig 로드 실패");
                return;
            }

            // 핸들러 등록
            _handlers[GachaType.Equipment] = new EquipmentGachaHandler(gachaEquipmentTable, equipmentTable, _levelConfig, _equipmentService);
            _handlers[GachaType.Drone] = new DroneGachaHandler(gachaDroneTable, droneTable);

            await LoadAsync();
            IsInitialized = true;
        }

        public int GetGachaLevel(GachaType type)
        {
            var totalCount = GetTotalCount(type);
            return _levelConfig.GetLevel(type, totalCount);
        }

        public int GetTotalCount(GachaType type)
        {
            return _totalCounts.TryGetValue(type, out var count) ? count : 0;
        }

        public int GetRequiredCountForNextLevel(GachaType type)
        {
            if (_levelConfig == null)
                return 0;

            int currentLevel = GetGachaLevel(type);
            int maxLevel = _levelConfig.GetMaxLevel(type);

            if (currentLevel >= maxLevel)
                return 0; // 이미 최대 레벨

            return _levelConfig.GetRequiredCountForLevel(type, currentLevel + 1);
        }

        public int GetRequiredCountForLevel(GachaType type, int level)
        {
            if (_levelConfig == null)
                return 0;

            return _levelConfig.GetRequiredCountForLevel(type, level);
        }

        public List<GachaResult> Pull(GachaType type, int count, BigDouble cost, CurrencyType currencyType)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GachaService] 초기화되지 않았습니다.");
                return new List<GachaResult>();
            }

            // 핸들러 가져오기
            if (!_handlers.TryGetValue(type, out var handler))
            {
                Debug.LogError($"[GachaService] {type} 타입의 핸들러를 찾을 수 없습니다.");
                return new List<GachaResult>();
            }

            // 비용 차감
            if (!_currencyService.TryConsume(currencyType, cost, $"Gacha_{type}_{count}"))
            {
                Debug.LogWarning($"[GachaService] 재화 부족: {currencyType} {cost} 필요");
                return new List<GachaResult>();
            }

            var currentLevel = GetGachaLevel(type);
            var results = handler.Pull(currentLevel, count);

            // 결과를 인벤토리에 추가
            foreach (var result in results)
            {
                handler.AddToInventory(result);
            }

            // 가챠 횟수 증가
            _totalCounts[type] = GetTotalCount(type) + count;

            // 이벤트 발행
            _eventBus?.Publish(new GachaPullEvent
            {
                Type = type,
                Count = count,
                Results = results
            });

            return results;
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new GachaSaveData();
                data.EquipmentTotalCount = GetTotalCount(GachaType.Equipment);
                data.DroneTotalCount = GetTotalCount(GachaType.Drone);

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[GachaService] 저장 완료: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GachaService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[GachaService] 저장 파일이 없어 기본값으로 초기화합니다.");
                    _totalCounts.Clear();
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<GachaSaveData>(json);

                if (data != null)
                {
                    _totalCounts[GachaType.Equipment] = data.EquipmentTotalCount;
                    _totalCounts[GachaType.Drone] = data.DroneTotalCount;
                }
                else
                {
                    // JSON 파싱은 성공했지만 data가 null인 경우 기본값으로 초기화
                    Debug.LogWarning("[GachaService] 저장 데이터가 null입니다. 기본값으로 초기화합니다.");
                    _totalCounts.Clear();
                    await SaveAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GachaService] 로드 실패: {ex.Message}");
                _totalCounts.Clear();
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }

    /// <summary>
    /// 가챠 뽑기 이벤트
    /// </summary>
    public struct GachaPullEvent
    {
        public GachaType Type;
        public int Count;
        public List<GachaResult> Results;
    }
}