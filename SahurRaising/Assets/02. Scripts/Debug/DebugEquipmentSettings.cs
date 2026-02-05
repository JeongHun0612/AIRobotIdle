using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Debug용 장비 컨트롤 ScriptableObject
    /// 에디터에서 장비를 추가/장착/레벨업할 수 있도록 하는 디버그 도구
    /// </summary>
    [CreateAssetMenu(fileName = "DebugEquipmentSettings", menuName = "SahurRaising/Debug/DebugEquipmentSettings")]
    public class DebugEquipmentSettings : ScriptableObject
    {
        [Header("장비 추가 설정")]
        [Tooltip("추가할 장비 타입")]
        public EquipmentType AddEquipmentType = EquipmentType.Processor;

        [Tooltip("추가할 장비 코드 (타입 선택 후 드롭다운에서 선택)")]
        public string AddEquipmentCode = "";

        [Tooltip("추가할 장비 개수")]
        [Range(1, 100)]
        public int AddCount = 1;

        [Header("장비 장착 설정")]
        [Tooltip("장착할 장비 타입")]
        public EquipmentType EquipType = EquipmentType.Processor;

        [Tooltip("장착할 장비 코드 (타입 선택 후 드롭다운에서 선택)")]
        public string EquipCode = "";

        [Header("장비 레벨업 설정")]
        [Tooltip("레벨업할 장비 타입")]
        public EquipmentType LevelUpType = EquipmentType.Processor;

        [Tooltip("레벨업할 장비 코드 (타입 선택 후 드롭다운에서 선택)")]
        public string LevelUpCode = "";

        [Header("장비 승급 설정")]
        [Tooltip("승급할 장비 타입")]
        public EquipmentType AdvanceType = EquipmentType.Processor;
    }
}