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
        private const int MaxGachaLevel = 9;
        private const int GachaLevelUpInterval = 100; // 100개마다 레벨 업

        private readonly IResourceService _resourceService;
        private readonly ICurrencyService _currencyService;
        private readonly IEquipmentService _equipmentService;
        private readonly IEventBus _eventBus;

        private GachaEquipmentTable _equipmentTable;
        private GachaDroneTable _droneTable;
        private EquipmentTable _equipmentDataTable;
        private DroneTable _droneDataTable;
        private GachaLevelConfig _levelConfig;

        // 타입별 누적 뽑기 개수 관리 (Dictionary로 확장성 확보)
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
            _equipmentTable = await _resourceService.LoadTableAsync<GachaEquipmentTable>("GachaEquipmentTable");
            _droneTable = await _resourceService.LoadTableAsync<GachaDroneTable>("GachaDroneTable");
            _equipmentDataTable = await _resourceService.LoadTableAsync<EquipmentTable>("EquipmentTable");
            _droneDataTable = await _resourceService.LoadTableAsync<DroneTable>("DroneTable");
            _levelConfig = await _resourceService.LoadAssetAsync<GachaLevelConfig>("GachaLevelConfig");

            if (_equipmentTable == null || _droneTable == null)
            {
                Debug.LogError("[GachaService] 가챠 테이블 로드 실패");
                return;
            }

            if (_levelConfig == null)
            {
                Debug.LogError("[GachaService] GachaLevelConfig 로드 실패");
                return;
            }

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

        public BigDouble GetCost(GachaType type, int count)
        {
            // TODO: 실제 비용 테이블에서 가져오거나 상수로 정의
            var costPerDraw = type == GachaType.Equipment ? 100 : 50; // 예시 값
            return new BigDouble(costPerDraw * count);
        }

        public async UniTask<List<GachaResult>> DrawAsync(GachaType type, int count, BigDouble cost, CurrencyType currencyType)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GachaService] 초기화되지 않았습니다.");
                return new List<GachaResult>();
            }

            // 비용 차감
            if (!_currencyService.TryConsume(currencyType, cost, $"Gacha_{type}_{count}"))
            {
                Debug.LogWarning($"[GachaService] 재화 부족: {currencyType} {cost} 필요");
                return new List<GachaResult>();
            }

            var results = new List<GachaResult>();
            var currentLevel = GetGachaLevel(type);

            for (int i = 0; i < count; i++)
            {
                GachaResult result;
                if (type == GachaType.Equipment)
                {
                    result = DrawEquipment(currentLevel);
                    _equipmentService?.AddToInventory(result.ItemCode, 1);
                }
                else
                {
                    result = DrawDrone(currentLevel);
                    // TODO: 드론 인벤토리에 추가하는 로직 필요
                }

                results.Add(result);
            }

            // 가챠 횟수 증가 (레벨 계산용) - 한 번에 처리
            _totalCounts[type] = GetTotalCount(type) + count;

            // 이벤트 발행
            _eventBus?.Publish(new GachaDrawEvent
            {
                Type = type,
                Count = count,
                Results = results
            });

            return results;
        }

        private GachaResult DrawEquipment(int level)
        {
            // 레벨에 맞는 확률 테이블 가져오기
            var probabilities = new Dictionary<EquipmentGrade, float>();
            float totalProb = 0f;

            // 최대 레벨 동적 확인
            var maxLevel = _levelConfig.GetMaxLevel(GachaType.Equipment);
            var clampedLevel = Mathf.Clamp(level, 1, maxLevel);

            foreach (var row in _equipmentTable.Rows)
            {
                if (row.Probabilities == null || clampedLevel > row.Probabilities.Count)
                    continue;

                var prob = row.Probabilities[clampedLevel - 1]; // level은 1부터 시작, 인덱스는 0부터
                if (prob > 0)
                {
                    probabilities[row.Grade] = prob;
                    totalProb += prob;
                }
            }

            // 확률에 따라 등급 결정
            var random = UnityEngine.Random.Range(0f, totalProb);
            float accumulated = 0f;
            EquipmentGrade selectedGrade = EquipmentGrade.F;

            foreach (var kvp in probabilities)
            {
                accumulated += kvp.Value;
                if (random <= accumulated)
                {
                    selectedGrade = kvp.Key;
                    break;
                }
            }

            // 등급에 맞는 장비 랜덤 선택
            //var equipmentList = _equipmentDataTable.GetByGrade(selectedGrade);
            List<EquipmentRow> equipmentList = null;
            if (equipmentList == null || equipmentList.Count == 0)
            {
                Debug.LogError($"[GachaService] 등급 {selectedGrade}에 해당하는 장비가 없습니다.");
                return new GachaResult { Type = GachaType.Equipment, ItemCode = "", Grade = selectedGrade };
            }

            var randomIndex = UnityEngine.Random.Range(0, equipmentList.Count);
            var selectedEquipment = equipmentList[randomIndex];

            return new GachaResult
            {
                Type = GachaType.Equipment,
                ItemCode = selectedEquipment.Code,
                Grade = selectedGrade
            };
        }

        private GachaResult DrawDrone(int level)
        {
            // 드론 뽑기 로직 (장비와 유사하지만 드론 ID 기반)
            // TODO: 드론 테이블 구조에 맞게 구현 필요
            var probabilities = new Dictionary<string, float>();
            float totalProb = 0f;

            foreach (var row in _droneTable.Rows)
            {
                if (row.Probabilities == null || level > row.Probabilities.Count)
                    continue;

                var prob = row.Probabilities[level - 1];
                if (prob > 0)
                {
                    probabilities[row.ID] = prob;
                    totalProb += prob;
                }
            }

            var random = UnityEngine.Random.Range(0f, totalProb);
            float accumulated = 0f;
            string selectedDroneID = "";

            foreach (var kvp in probabilities)
            {
                accumulated += kvp.Value;
                if (random <= accumulated)
                {
                    selectedDroneID = kvp.Key;
                    break;
                }
            }

            return new GachaResult
            {
                Type = GachaType.Drone,
                ItemCode = selectedDroneID,
                Grade = null
            };
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new GachaSaveData();

                // Dictionary의 모든 타입 저장
                data.EquipmentTotalCount = GetTotalCount(GachaType.Equipment);
                data.DroneTotalCount = GetTotalCount(GachaType.Drone);

                // 새로운 타입이 추가되면 여기에 추가 필요 (또는 Dictionary를 직렬화)

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
    public struct GachaDrawEvent
    {
        public GachaType Type;
        public int Count;
        public List<GachaResult> Results;
    }
}