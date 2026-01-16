using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SahurRaising.Core
{
    public class DroneService : IDroneService
    {
        private const string DRONETABLE_KEY = "DroneTable";
        private const string SaveFileName = "drone.json";

        // 강화 필요 개수 상수 (현재 4개 고정)
        private const int REQUIRED_COUNT_FOR_UPGRADE = 4;

        // 9레벨 특별 합성 상수
        private const string DRONE_LEVEL_9_ID = "9";
        private const float ADVANCE_TO_LEVEL_10_RATE = 0.2f;
        private readonly string[] DRONE_LEVEL_10_IDS = { "10A", "10B", "10C" };

        private string _equippedID;                                                      // 장착 관리
        private readonly Dictionary<string, DroneInventoryInfo> _inventory = new();      // 인벤토리 관리 (드론 ID -> (레벨, 개수))
        private readonly HashSet<string> _seenIDs = new();                               // NEW 여부 판단용 (본 적 있는 드론)

        private readonly IResourceService _resourceService;

        private DroneTable _droneTable;
        private Dictionary<string, DroneRow> _droneByID;

        public bool IsInitialized { get; private set; }

        public DroneService(IResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[DroneService] 이미 초기화되었습니다.");
                return;
            }

            try
            {
                Debug.Log("[DroneService] DroneTable 로드 시작...");

                if (_resourceService == null)
                {
                    Debug.LogError("[DroneService] IResourceService가 null입니다.");
                    return;
                }

                // Addressables 키는 "DroneTable" 또는 실제 등록된 키를 사용
                _droneTable = await _resourceService.LoadAssetAsync<DroneTable>(DRONETABLE_KEY);

                if (_droneTable == null)
                {
                    Debug.LogError("[DroneService] DroneTable 로드 실패");
                    return;
                }

                // 인덱스 빌드
                BuildIndexes();

                IsInitialized = true;

                // 저장 데이터 로드
                await LoadAsync();

                Debug.Log($"[DroneService] 초기화 완료. 총 {_droneTable.Rows.Count}개 드론 로드됨");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DroneService] 초기화 중 오류: {ex.Message}");
                IsInitialized = false;
            }
        }

        /// <summary>
        /// ID별 Dictionary 인덱스를 빌드합니다.
        /// </summary>
        private void BuildIndexes()
        {
            _droneByID = new Dictionary<string, DroneRow>();

            foreach (var drone in _droneTable.Rows)
            {
                // ID별 인덱스
                if (!string.IsNullOrEmpty(drone.ID))
                    _droneByID[drone.ID] = drone;
            }
        }

        public IReadOnlyList<DroneRow> GetAll()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[DroneService] 아직 초기화되지 않았습니다.");
                return new List<DroneRow>();
            }

            return _droneTable?.Rows ?? new List<DroneRow>();
        }

        public bool TryGetByID(string id, out DroneRow drone)
        {
            drone = default;

            if (!IsInitialized)
            {
                Debug.LogWarning("[DroneService] 아직 초기화되지 않았습니다.");
                return false;
            }

            if (_droneByID == null || string.IsNullOrEmpty(id))
                return false;

            return _droneByID.TryGetValue(id, out drone);
        }

        public string GetEquippedID()
        {
            return string.IsNullOrEmpty(_equippedID) ? null : _equippedID;
        }

        public bool Equip(string droneID)
        {
            if (string.IsNullOrEmpty(droneID))
            {
                Debug.LogWarning($"[DroneService] 드론 ID가 비어있습니다.");
                return false;
            }

            // 드론 ID 유효성 검사
            if (!TryGetByID(droneID, out var drone))
            {
                Debug.LogWarning($"[DroneService] 존재하지 않는 드론 ID: {droneID}");
                return false;
            }

            // 인벤토리에 드론이 있는지 확인
            if (!_inventory.TryGetValue(droneID, out var inventoryInfo) || !inventoryInfo.IsOwned)
            {
                Debug.LogWarning($"[DroneService] Equip: 인벤토리에 없는 드론입니다: {droneID}");
                return false;
            }

            // 드론 장착
            _equippedID = droneID;

            Debug.Log($"[DroneService] Equip 완료: {droneID} (보유 레벨: {inventoryInfo.Level}, 수량: {inventoryInfo.Count})");

            return true;
        }

        public bool Unequip()
        {
            if (string.IsNullOrEmpty(_equippedID))
            {
                Debug.LogWarning($"[DroneService] Unequip: 이미 비어 있는 슬롯입니다.");
                return false;
            }

            var id = _equippedID;
            // 드론 해제
            _equippedID = null;

            Debug.Log($"[DroneService] Unequip 완료: {id}");
            return true;
        }

        public bool AddToInventory(string droneID, int count = 1)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[DroneService] 아직 초기화되지 않았습니다.");
                return false;
            }

            if (string.IsNullOrEmpty(droneID))
            {
                Debug.LogWarning("[DroneService] AddToInventory: droneID가 비어 있습니다.");
                return false;
            }

            if (count <= 0)
            {
                Debug.LogWarning("[DroneService] AddToInventory: count는 1 이상이어야 합니다.");
                return false;
            }

            // 유효한 드론 ID인지 확인
            if (!TryGetByID(droneID, out _))
            {
                Debug.LogWarning($"[DroneService] AddToInventory: 존재하지 않는 드론 ID: {droneID}");
                return false;
            }

            if (_inventory.TryGetValue(droneID, out var info))
            {
                // 기존 레벨 유지, 개수만 증가
                var newCount = info.Count + count;
                _inventory[droneID] = new DroneInventoryInfo(droneID, info.Level, newCount, true);
            }
            else
            {
                // 처음 획득: 레벨은 기본값(예: 1)로 시작, 개수는 count
                const int defaultLevel = 1;
                _inventory[droneID] = new DroneInventoryInfo(droneID, defaultLevel, count, true);
            }

            Debug.Log($"[DroneService] AddToInventory: {droneID}, +{count}");
            return true;
        }

        public DroneInventoryInfo GetInventoryInfo(string droneID)
        {
            if (string.IsNullOrEmpty(droneID))
                return new DroneInventoryInfo(string.Empty, 0, 0, false);

            return _inventory.TryGetValue(droneID, out var info)
                ? info
                : new DroneInventoryInfo(string.Empty, 0, 0, false); // 보유 X
        }

        public bool LevelUp(string droneID)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[DroneService] 아직 초기화되지 않았습니다.");
                return false;
            }

            if (string.IsNullOrEmpty(droneID))
            {
                Debug.LogWarning("[DroneService] LevelUp: droneID가 비어 있습니다.");
                return false;
            }

            // 인벤토리에서 드론 정보 가져오기
            if (!_inventory.TryGetValue(droneID, out var info))
            {
                Debug.LogWarning($"[DroneService] LevelUp: 인벤토리에 없는 드론입니다: {droneID}");
                return false;
            }

            // 레벨 증가
            int newLevel = info.Level + 1;
            _inventory[droneID] = new DroneInventoryInfo(droneID, newLevel, info.Count, info.IsOwned);

            Debug.Log($"[DroneService] LevelUp 완료: {droneID}, 레벨 {info.Level} -> {newLevel}");
            return true;
        }

        public int GetRequiredCountForAdvance()
        {
            return REQUIRED_COUNT_FOR_UPGRADE;
        }

        public List<AdvanceResult> AdvanceAllAvailable()
        {
            var results = new List<AdvanceResult>();

            if (!IsInitialized)
            {
                Debug.LogWarning("[DroneService] 아직 초기화되지 않았습니다.");
                return results;
            }

            int requiredCount = GetRequiredCountForAdvance();

            // 모든 드론 가져오기
            var droneList = GetAll();

            if (droneList == null || droneList.Count == 0)
                return results;

            AdvanceResult? lastAdvanceResult = null;
            bool hasLevel9Advance = false; // 9레벨 합성이 발생했는지 여부

            // 하위 등급부터 상위 등급으로 순회하며 승급 처리
            for (int i = 0; i < droneList.Count - 1; i++)
            {
                var drone = droneList[i];

                if (string.IsNullOrEmpty(drone.ID))
                    continue;

                if (!_inventory.TryGetValue(drone.ID, out var info))
                    continue;

                // requiredCount 보다 보유 갯수가 적으면 스킵
                if (info.Count < requiredCount)
                    continue;

                // 승급 가능한 횟수 계산
                int advanceCount = info.Count / requiredCount;
                if (advanceCount <= 0)
                    continue;

                // 이전 레벨 저장 및 IsOwned 저장
                int beforeLevel = info.Level;
                bool isOwned = info.IsOwned;  // IsOwned는 유지

                // 9레벨 드론 특별 처리: 확률 기반 합성
                if (drone.ID == DRONE_LEVEL_9_ID)
                {
                    hasLevel9Advance = true;

                    // 9레벨 드론 합성 처리
                    int level9ObtainedCount = 0;
                    int totalUsed = 0;

                    // 각 획득 결과를 Dictionary로 집계 (ID별 개수)
                    var level10Results = new Dictionary<string, int>();

                    for (int attempt = 0; attempt < advanceCount; attempt++)
                    {
                        // 20% 확률로 10레벨 드론 획득
                        bool isLevel10Success = Random.Range(0f, 1f) < ADVANCE_TO_LEVEL_10_RATE;
                        string selectedID = drone.ID;

                        if (isLevel10Success)
                        {
                            // 10A, 10B, 10C 중 랜덤 선택
                            selectedID = DRONE_LEVEL_10_IDS[Random.Range(0, DRONE_LEVEL_10_IDS.Length)];
                            AddToInventory(selectedID, 1);

                            // 결과 집계
                            if (level10Results.ContainsKey(selectedID))
                                level10Results[selectedID]++;
                            else
                                level10Results[selectedID] = 1;

                            Debug.Log($"[DroneService] 9레벨 드론 합성 성공: {selectedID} 획득 (20% 확률)");
                        }
                        else
                        {
                            // 80% 확률: 9레벨 드론 1개 획득
                            level9ObtainedCount++;
                            Debug.Log("[DroneService] 9레벨 드론 합성: 9레벨 드론 1개 획득 (80% 확률)");
                        }

                        totalUsed += requiredCount;
                    }

                    // 사용된 드론 개수만큼 감소
                    int remainingCount = info.Count - totalUsed;

                    // 80% 확률로 획득한 9레벨 드론 추가
                    remainingCount += level9ObtainedCount;

                    _inventory[drone.ID] = new DroneInventoryInfo(drone.ID, beforeLevel, remainingCount, isOwned);

                    // 9레벨 드론 획득 결과 추가
                    if (level9ObtainedCount > 0 && TryGetByID(DRONE_LEVEL_9_ID, out var level9Drone))
                    {
                        results.Add(new AdvanceResult
                        {
                            Type = GachaType.Drone,
                            ItemCode = DRONE_LEVEL_9_ID,
                            GradeKey = DRONE_LEVEL_9_ID,
                            Icon = level9Drone.Icon,
                            Count = level9ObtainedCount
                        });
                    }

                    // 10레벨 드론 결과 추가
                    foreach (var kvp in level10Results)
                    {
                        if (TryGetByID(kvp.Key, out var resultDrone))
                        {
                            results.Add(new AdvanceResult
                            {
                                Type = GachaType.Drone,
                                ItemCode = kvp.Key,
                                GradeKey = kvp.Key,
                                Icon = resultDrone.Icon,
                                Count = kvp.Value
                            });
                        }
                    }
                }
                else
                {
                    // 다음 드론
                    var nextDrone = droneList[i + 1];

                    if (string.IsNullOrEmpty(nextDrone.ID))
                        continue;

                    string nextDroneID = nextDrone.ID;

                    // 승급 수행: 하위 등급 개수 감소, 상위 등급 개수 증가
                    int remainingCount = info.Count % requiredCount;
                    int newNextDroneCount = advanceCount;

                    // 인벤토리 업데이트: 하위 등급 개수 감소
                    _inventory[drone.ID] = new DroneInventoryInfo(drone.ID, beforeLevel, remainingCount, isOwned);

                    // 인벤토리 업데이트: 상위 등급 개수 증가
                    AddToInventory(nextDroneID, newNextDroneCount);

                    // 상위 등급 드론 정보 저장
                    lastAdvanceResult = new AdvanceResult
                    {
                        Type = GachaType.Drone,
                        ItemCode = nextDroneID,
                        GradeKey = nextDroneID,
                        Icon = nextDrone.Icon,
                        Count = newNextDroneCount
                    };
                }
            }

            // 9레벨 합성이 없었다면 1~8레벨 최상위 결과만 반환
            if (!hasLevel9Advance && lastAdvanceResult.HasValue)
            {
                results.Add(lastAdvanceResult.Value);
            }

            return results;
        }

        public bool IsNewDrone(string droneID)
        {
            // 인벤토리에 있고, 아직 본 적 없는 ID면 NEW
            if (string.IsNullOrEmpty(droneID))
                return false;

            if (!_inventory.TryGetValue(droneID, out var info))
                return false;

            if (info.Count <= 0)
                return false;

            return !_seenIDs.Contains(droneID);
        }

        public void MarkAsSeen(string droneID)
        {
            if (string.IsNullOrEmpty(droneID))
                return;

            _seenIDs.Add(droneID);
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new DroneSaveData();

                // 장착 정보 저장
                data.EquippedDroneID = GetEquippedID();

                // 인벤토리 정보 저장
                foreach (var pair in _inventory)
                {
                    if (pair.Value.IsOwned)
                    {
                        data.Inventory.Add(pair.Value);
                    }
                }

                // SeenIDs 저장
                data.SeenIDs.Clear();
                data.SeenIDs.AddRange(_seenIDs);

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[DroneService] 저장 완료: {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DroneService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[DroneService] 저장 파일이 없어 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<DroneSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[DroneService] 저장 데이터가 null입니다. 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                // 장착 정보 로드
                _equippedID = null;
                if (!string.IsNullOrEmpty(data.EquippedDroneID))
                    _equippedID = data.EquippedDroneID;

                // 인벤토리 정보 로드
                _inventory.Clear();
                if (data.Inventory != null)
                {
                    foreach (var info in data.Inventory)
                    {
                        if (!string.IsNullOrEmpty(info.ID))
                        {
                            // 드론 ID 유효성 검사
                            if (TryGetByID(info.ID, out _))
                            {
                                // 구버전 데이터 호환성을 위해 IsOwned가 없으면 Count > 0 또는 Level > 1이면 true
                                bool isOwned = info.IsOwned || info.Count > 0 || info.Level > 1;
                                _inventory[info.ID] = new DroneInventoryInfo(info.ID, info.Level, info.Count, isOwned);
                            }
                            else
                            {
                                Debug.LogWarning($"[DroneService] 존재하지 않는 드론 ID를 건너뜁니다: {info.ID}");
                            }
                        }
                    }
                }

                // 이미 본 드론(NEW가 꺼진 드론) ID 목록 로드
                _seenIDs.Clear();
                if (data.SeenIDs != null)
                {
                    foreach (var id in data.SeenIDs)
                    {
                        if (!string.IsNullOrEmpty(id))
                            _seenIDs.Add(id);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DroneService] 로드 실패: {ex.Message}");
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }
}
