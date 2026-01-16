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
        private readonly IDroneService _droneService;
        private readonly IEventBus _eventBus;

        private GachaLevelConfig _levelConfig;
        private GachaButtonConfig _gachaButtonConfig;

        // 타입별 핸들러 관리
        private readonly Dictionary<GachaType, IGachaHandler> _handlers = new();

        // 타입별 가챠 데이터 관리
        private readonly Dictionary<GachaType, GachaTypeSaveData> _gachaData = new();

        public bool IsInitialized { get; private set; }
        public GachaLevelConfig LevelConfig => _levelConfig;
        public GachaButtonConfig GachaButtonConfig => _gachaButtonConfig;

        public GachaService(
            IResourceService resourceService,
            ICurrencyService currencyService,
            IEquipmentService equipmentService,
            IDroneService droneService,
            IEventBus eventBus)
        {
            _resourceService = resourceService;
            _currencyService = currencyService;
            _equipmentService = equipmentService;
            _droneService = droneService;
            _eventBus = eventBus;
        }

        public async UniTask InitializeAsync()
        {
            var gachaEquipmentTable = await _resourceService.LoadTableAsync<GachaEquipmentTable>("GachaEquipmentTable");
            var gachaDroneTable = await _resourceService.LoadTableAsync<GachaDroneTable>("GachaDroneTable");
            var equipmentTable = await _resourceService.LoadTableAsync<EquipmentTable>("EquipmentTable");
            var droneTable = await _resourceService.LoadTableAsync<DroneTable>("DroneTable");
            _levelConfig = await _resourceService.LoadAssetAsync<GachaLevelConfig>("GachaLevelConfig");
            _gachaButtonConfig = await _resourceService.LoadAssetAsync<GachaButtonConfig>("GachaButtonConfig");

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
            _handlers[GachaType.Drone] = new DroneGachaHandler(gachaDroneTable, droneTable, _droneService);

            await LoadAsync();
            IsInitialized = true;
        }

        public int GetGachaLevel(GachaType type)
        {
            return _gachaData.TryGetValue(type, out var data) ? data.Level : 0;
        }

        public int GetGachaCount(GachaType type)
        {
            return _gachaData.TryGetValue(type, out var data) ? data.Count : 0;
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

        public CurrencyType GetCurrencyType(GachaType type)
        {
            switch (type)
            {
                case GachaType.Equipment:
                    return CurrencyType.Diamond;
                case GachaType.Drone:
                    return CurrencyType.Diamond;
                default:
                    Debug.LogWarning($"[GachaService] 알 수 없는 가챠 타입: {type}. 기본값 Diamond를 반환합니다.");
                    return CurrencyType.Diamond;
            }
        }

        public List<GachaResult> Pull(GachaType type, int count, BigDouble cost)
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
            var currencyType = GetCurrencyType(type);
            if (!_currencyService.TryConsume(currencyType, cost, $"Gacha_{type}_{count}"))
            {
                Debug.LogWarning($"[GachaService] 재화 부족: {currencyType} {cost} 필요");
                return new List<GachaResult>();
            }

            int currentLevel = GetGachaLevel(type);
            int currentCount = GetGachaCount(type);

            var results = handler.Pull(currentLevel, count);

            // 결과를 인벤토리에 추가
            foreach (var result in results)
            {
                handler.AddToInventory(result);
            }

            // 가챠 횟수 증가
            int newCount = currentCount + count;

            // 현재 레벨에서 다음 레벨로 가기 위해 필요한 개수
            int nextLevelRequiredCount = _levelConfig.GetRequiredCountForLevel(type, currentLevel + 1);
            if (newCount >= nextLevelRequiredCount)
            {
                newCount = newCount - nextLevelRequiredCount;
                currentLevel++;
            }

            int maxLevel = _levelConfig.GetMaxLevel(type);
            if (currentLevel >= maxLevel)
            {
                newCount = 0;
            }

            // 데이터 업데이트
            _gachaData[type] = new GachaTypeSaveData(type, newCount, currentLevel);

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
                data.GachaDataList.Clear();

                // 모든 GachaType에 대해 데이터 저장
                foreach (GachaType type in System.Enum.GetValues(typeof(GachaType)))
                {
                    if (_gachaData.TryGetValue(type, out var gachaData))
                    {
                        data.GachaDataList.Add(gachaData);
                    }
                    else
                    {
                        // 데이터가 없는 경우 기본값으로 저장
                        int count = 0;
                        int level = 1;
                        var defaultData = new GachaTypeSaveData(type, count, level);
                        _gachaData[type] = defaultData;
                        data.GachaDataList.Add(defaultData);
                    }
                }

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
                    _gachaData.Clear();
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<GachaSaveData>(json);

                if (data != null)
                {
                    _gachaData.Clear();

                    // List에서 데이터 로드
                    if (data.GachaDataList != null && data.GachaDataList.Count > 0)
                    {
                        foreach (var gachaData in data.GachaDataList)
                        {
                            // 레벨이 없거나 0이면 계산
                            int level = gachaData.Level > 0 ? gachaData.Level : 1;
                            _gachaData[gachaData.Type] = new GachaTypeSaveData(gachaData.Type, gachaData.Count, level);
                        }
                    }
                    else
                    {
                        // JsonUtility는 없는 필드를 무시하므로, 이 경우는 빈 리스트로 처리
                        Debug.LogWarning("[GachaService] GachaDataList가 비어있습니다. 기본값으로 초기화합니다.");
                        _gachaData.Clear();
                        await SaveAsync();
                    }
                }
                else
                {
                    // JSON 파싱은 성공했지만 data가 null인 경우 기본값으로 초기화
                    Debug.LogWarning("[GachaService] 저장 데이터가 null입니다. 기본값으로 초기화합니다.");
                    _gachaData.Clear();
                    await SaveAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GachaService] 로드 실패: {ex.Message}");
                _gachaData.Clear();
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