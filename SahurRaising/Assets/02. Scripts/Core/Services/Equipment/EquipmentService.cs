using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SahurRaising.Core
{
    public class EquipmentService : IEquipmentService
    {
        private const string EQUIPMENTTABLE_KEY = "EquipmentTable";
        private const string SaveFileName = "equipment.json";

        // 강화 필요 개수 상수 (현재 4개 고정)
        private const int REQUIRED_COUNT_FOR_UPGRADE = 4;

        private readonly Dictionary<EquipmentType, string> _equipped = new();               // 장착 관리
        private readonly Dictionary<string, EquipmentInventoryInfo> _inventory = new();     // 인벤토리 관리 (장비 코드 -> (레벨, 개수))
        private readonly HashSet<string> _seenCodes = new();                                // NEW 여부 판단용 (본 적 있는 장비)

        private readonly IResourceService _resourceService;

        private EquipmentTable _equipmentTable;
        private Dictionary<EquipmentType, List<EquipmentRow>> _equipmentByType;
        private Dictionary<string, EquipmentRow> _equipmentByCode;

        public bool IsInitialized { get; private set; }

        public EquipmentService(IResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 이미 초기화되었습니다.");
                return;
            }

            try
            {
                Debug.Log("[EquipmentService] EquipmentTable 로드 시작...");

                if (_resourceService == null)
                {
                    Debug.LogError("[EquipmentService] IResourceService가 null입니다.");
                    return;
                }

                // Addressables 키는 "EquipmentTable" 또는 실제 등록된 키를 사용
                _equipmentTable = await _resourceService.LoadAssetAsync<EquipmentTable>(EQUIPMENTTABLE_KEY);

                if (_equipmentTable == null)
                {
                    Debug.LogError("[EquipmentService] EquipmentTable 로드 실패");
                    return;
                }

                // 인덱스 빌드
                BuildIndexes();

                IsInitialized = true;

                // 저장 데이터 로드
                await LoadAsync();

                Debug.Log($"[EquipmentService] 초기화 완료. 총 {_equipmentTable.Rows.Count}개 장비 로드됨");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EquipmentService] 초기화 중 오류: {ex.Message}");
                IsInitialized = false;
            }
        }

        /// <summary>
        /// 타입별 Dictionary 인덱스를 빌드합니다.
        /// </summary>
        private void BuildIndexes()
        {
            _equipmentByType = new Dictionary<EquipmentType, List<EquipmentRow>>();
            _equipmentByCode = new Dictionary<string, EquipmentRow>();

            foreach (var equipment in _equipmentTable.Rows)
            {
                // 타입별 인덱스
                if (!_equipmentByType.ContainsKey(equipment.Type))
                    _equipmentByType[equipment.Type] = new List<EquipmentRow>();

                _equipmentByType[equipment.Type].Add(equipment);

                // 코드별 인덱스
                if (!string.IsNullOrEmpty(equipment.Code))
                    _equipmentByCode[equipment.Code] = equipment;
            }
        }

        public IReadOnlyList<EquipmentRow> GetAll()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 아직 초기화되지 않았습니다.");
                return new List<EquipmentRow>();
            }

            return _equipmentTable?.Rows ?? new List<EquipmentRow>();
        }

        public IReadOnlyList<EquipmentRow> GetByType(EquipmentType type)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 아직 초기화되지 않았습니다.");
                return new List<EquipmentRow>();
            }

            if (_equipmentByType == null || !_equipmentByType.ContainsKey(type))
                return new List<EquipmentRow>();

            return _equipmentByType[type];
        }

        public bool TryGetByCode(string code, out EquipmentRow equipment)
        {
            equipment = default;

            if (!IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 아직 초기화되지 않았습니다.");
                return false;
            }

            if (_equipmentByCode == null || string.IsNullOrEmpty(code))
                return false;

            return _equipmentByCode.TryGetValue(code, out equipment);
        }

        public string GetEquippedCode(EquipmentType type)
        {
            return _equipped.TryGetValue(type, out var code) ? code : null;
        }

        public bool Equip(EquipmentType type, string equipmentCode)
        {
            if (string.IsNullOrEmpty(equipmentCode))
            {
                Debug.LogWarning($"[EquipmentService] 장비 코드가 비어있습니다.");
                return false;
            }

            // 장비 코드 유효성 검사
            if (!TryGetByCode(equipmentCode, out var equipment))
            {
                Debug.LogWarning($"[EquipmentService] 존재하지 않는 장비 코드: {equipmentCode}");
                return false;
            }

            // 인벤토리에 장비가 있는지 확인
            if (!_inventory.TryGetValue(equipmentCode, out var inventoryInfo) || !inventoryInfo.IsOwned)
            {
                Debug.LogWarning($"[EquipmentService] Equip: 인벤토리에 없는 장비입니다: {equipmentCode}");
                return false;
            }

            // 장비 장착
            _equipped[type] = equipmentCode;

            Debug.Log($"[EquipmentService] Equip 완료: {type} -> {equipmentCode} (보유 레벨: {inventoryInfo.Level}, 수량: {inventoryInfo.Count})");

            return true;
        }

        public bool Unequip(EquipmentType type)
        {
            if (!_equipped.TryGetValue(type, out var code) || string.IsNullOrEmpty(code))
            {
                Debug.LogWarning($"[EquipmentService] Unequip: 이미 비어 있는 슬롯입니다: {type}");
                return false;
            }

            // 장비 해제
            _equipped.Remove(type);

            Debug.Log($"[EquipmentService] Unequip 완료: {type} (코드: {code})");
            return true;
        }

        public bool AddToInventory(string equipmentCode, int count = 1)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 아직 초기화되지 않았습니다.");
                return false;
            }

            if (string.IsNullOrEmpty(equipmentCode))
            {
                Debug.LogWarning("[EquipmentService] AddToInventory: equipmentCode가 비어 있습니다.");
                return false;
            }

            if (count <= 0)
            {
                Debug.LogWarning("[EquipmentService] AddToInventory: count는 1 이상이어야 합니다.");
                return false;
            }

            // 유효한 장비 코드인지 확인
            if (!TryGetByCode(equipmentCode, out _))
            {
                Debug.LogWarning($"[EquipmentService] AddToInventory: 존재하지 않는 장비 코드: {equipmentCode}");
                return false;
            }

            if (_inventory.TryGetValue(equipmentCode, out var info))
            {
                // 기존 레벨 유지, 개수만 증가
                var newCount = info.Count + count;
                _inventory[equipmentCode] = new EquipmentInventoryInfo(equipmentCode, info.Level, newCount, true);
            }
            else
            {
                // 처음 획득: 레벨은 기본값(예: 1)로 시작, 개수는 count
                const int defaultLevel = 1;
                _inventory[equipmentCode] = new EquipmentInventoryInfo(equipmentCode, defaultLevel, count, true);
            }

            Debug.Log($"[EquipmentService] AddToInventory: {equipmentCode}, +{count}");
            return true;
        }

        public EquipmentInventoryInfo GetInventoryInfo(string equipmentCode)
        {
            if (string.IsNullOrEmpty(equipmentCode))
                return new EquipmentInventoryInfo(string.Empty, 0, 0, false);

            return _inventory.TryGetValue(equipmentCode, out var info)
                ? info
                : new EquipmentInventoryInfo(string.Empty, 0, 0, false); // 보유 X
        }

        public bool LevelUp(string equipmentCode)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 아직 초기화되지 않았습니다.");
                return false;
            }

            if (string.IsNullOrEmpty(equipmentCode))
            {
                Debug.LogWarning("[EquipmentService] LevelUp: equipmentCode가 비어 있습니다.");
                return false;
            }

            // 인벤토리에서 장비 정보 가져오기
            if (!_inventory.TryGetValue(equipmentCode, out var info))
            {
                Debug.LogWarning($"[EquipmentService] LevelUp: 인벤토리에 없는 장비입니다: {equipmentCode}");
                return false;
            }

            // 레벨 증가
            int newLevel = info.Level + 1;
            _inventory[equipmentCode] = new EquipmentInventoryInfo(equipmentCode, newLevel, info.Count, info.IsOwned);

            Debug.Log($"[EquipmentService] LevelUp 완료: {equipmentCode}, 레벨 {info.Level} -> {newLevel}");
            return true;
        }

        public int GetRequiredCountForAdvance()
        {
            return REQUIRED_COUNT_FOR_UPGRADE;
        }
        public AdvanceResult? AdvanceAllAvailable(EquipmentType type)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[EquipmentService] 아직 초기화되지 않았습니다.");
                return null;
            }

            int requiredCount = GetRequiredCountForAdvance();

            // 해당 타입의 모든 장비 가져오기
            var equipmentList = GetByType(type);

            if (equipmentList == null || equipmentList.Count == 0)
                return null;

            AdvanceResult? lastAdvanceResult = null;

            // 하위 등급부터 상위 등급으로 순회하며 승급 처리
            for (int i = 0; i < equipmentList.Count - 1; i++)
            {
                var equipment = equipmentList[i];

                if (string.IsNullOrEmpty(equipment.Code))
                    continue;

                if (!_inventory.TryGetValue(equipment.Code, out var info))
                    continue;

                // requiredCount 보다 보유 갯수가 적으면 스킵
                if (info.Count < requiredCount)
                    continue;

                // 다음 등급 장비
                var nextEquipment = equipmentList[i + 1];

                // 같은 등급이면 스킵
                if (nextEquipment.Grade <= equipment.Grade)
                    continue;

                if (string.IsNullOrEmpty(nextEquipment.Code))
                    continue;

                string nextGradeCode = nextEquipment.Code;

                // 승급 가능한 횟수 계산
                int advanceCount = info.Count / requiredCount;
                if (advanceCount <= 0)
                    continue;

                // 이전 레벨 저장 및 IsOwned 저장
                int beforeLevel = info.Level;
                bool isOwned = info.IsOwned;  // IsOwned는 유지

                // 승급 수행: 하위 등급 개수 감소, 상위 등급 개수 증가
                int remainingCount = info.Count % requiredCount;
                int newNextGradeCount = advanceCount;

                // 인벤토리 업데이트: 하위 등급 개수 감소
                _inventory[equipment.Code] = new EquipmentInventoryInfo(equipment.Code, beforeLevel, remainingCount, isOwned);

                // 인벤토리 업데이트: 상위 등급 개수 증가
                AddToInventory(nextGradeCode, newNextGradeCount);

                // 상위 등급 장비 정보 저장
                lastAdvanceResult = new AdvanceResult
                {
                    Type = GachaType.Equipment,
                    ItemCode = nextGradeCode,
                    GradeKey = nextEquipment.Grade.ToString(),
                    Icon = nextEquipment.Icon,
                    Count = newNextGradeCount
                };
            }

            return lastAdvanceResult;
        }

        public bool IsNewEquipment(string equipmentCode)
        {
            // 인벤토리에 있고, 아직 본 적 없는 코드면 NEW
            if (string.IsNullOrEmpty(equipmentCode))
                return false;

            if (!_inventory.TryGetValue(equipmentCode, out var info))
                return false;

            if (info.Count <= 0)
                return false;

            return !_seenCodes.Contains(equipmentCode);
        }

        public void MarkAsSeen(string equipmentCode)
        {
            if (string.IsNullOrEmpty(equipmentCode))
                return;

            _seenCodes.Add(equipmentCode);
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new EquipmentSaveData();

                // 장착 정보 저장
                data.EquippedProcessor = GetEquippedCode(EquipmentType.Processor);
                data.EquippedWheel = GetEquippedCode(EquipmentType.Wheel);
                data.EquippedBattery = GetEquippedCode(EquipmentType.Battery);
                data.EquippedAntenna = GetEquippedCode(EquipmentType.Antenna);
                data.EquippedMemory = GetEquippedCode(EquipmentType.Memory);
                data.EquippedRobotArm = GetEquippedCode(EquipmentType.RobotArm);

                // 인벤토리 정보 저장
                foreach (var pair in _inventory)
                {
                    if (pair.Value.IsOwned)
                    {
                        data.Inventory.Add(pair.Value);
                    }
                }

                // SeenCodes 저장
                data.SeenCodes.Clear();
                data.SeenCodes.AddRange(_seenCodes);

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[EquipmentService] 저장 완료: {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EquipmentService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[EquipmentService] 저장 파일이 없어 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<EquipmentSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[EquipmentService] 저장 데이터가 null입니다. 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                // 장착 정보 로드
                _equipped.Clear();
                if (!string.IsNullOrEmpty(data.EquippedProcessor))
                    _equipped[EquipmentType.Processor] = data.EquippedProcessor;
                if (!string.IsNullOrEmpty(data.EquippedWheel))
                    _equipped[EquipmentType.Wheel] = data.EquippedWheel;
                if (!string.IsNullOrEmpty(data.EquippedBattery))
                    _equipped[EquipmentType.Battery] = data.EquippedBattery;
                if (!string.IsNullOrEmpty(data.EquippedAntenna))
                    _equipped[EquipmentType.Antenna] = data.EquippedAntenna;
                if (!string.IsNullOrEmpty(data.EquippedMemory))
                    _equipped[EquipmentType.Memory] = data.EquippedMemory;
                if (!string.IsNullOrEmpty(data.EquippedRobotArm))
                    _equipped[EquipmentType.RobotArm] = data.EquippedRobotArm;

                // 인벤토리 정보 로드
                _inventory.Clear();
                if (data.Inventory != null)
                {
                    foreach (var info in data.Inventory)
                    {
                        if (!string.IsNullOrEmpty(info.Code))
                        {
                            // 장비 코드 유효성 검사
                            if (TryGetByCode(info.Code, out _))
                            {
                                // 구버전 데이터 호환성을 위해 IsOwned가 없으면 Count > 0 또는 Level > 1이면 true
                                bool isOwned = info.IsOwned || info.Count > 0 || info.Level > 1;
                                _inventory[info.Code] = new EquipmentInventoryInfo(info.Code, info.Level, info.Count, isOwned);
                            }
                            else
                            {
                                Debug.LogWarning($"[EquipmentService] 존재하지 않는 장비 코드를 건너뜁니다: {info.Code}");
                            }
                        }
                    }
                }

                //  이미 본 장비(NEW가 꺼진 장비) 코드 목록 로드
                _seenCodes.Clear();
                if (data.SeenCodes != null)
                {
                    foreach (var code in data.SeenCodes)
                    {
                        if (!string.IsNullOrEmpty(code))
                            _seenCodes.Add(code);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EquipmentService] 로드 실패: {ex.Message}");
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }
}
