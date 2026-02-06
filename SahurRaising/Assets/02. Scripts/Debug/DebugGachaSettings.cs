using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Debug용 가챠 레벨 및 카운트 관리 ScriptableObject
    /// 에디터에서 가챠 타입별 레벨과 카운트를 설정할 수 있는 디버그 도구
    /// </summary>
    [CreateAssetMenu(fileName = "DebugGachaSettings", menuName = "SahurRaising/Debug/DebugGachaSettings")]
    public class DebugGachaSettings : ScriptableObject
    {
        [System.Serializable]
        public class GachaDebugData
        {
            [Tooltip("가챠 타입")]
            public GachaType Type = GachaType.Equipment;

            [Tooltip("가챠 레벨 (1 이상)")]
            [Min(1)]
            public int Level = 1;

            [Tooltip("가챠 카운트 (누적 뽑기 개수)")]
            [Min(0)]
            public int Count = 0;
        }

        [Header("가챠 레벨 및 카운트 설정")]
        [Tooltip("가챠 타입별 레벨과 카운트를 설정합니다")]
        public GachaDebugData EquipmentGacha = new GachaDebugData { Type = GachaType.Equipment, Level = 1, Count = 0 };

        public GachaDebugData DroneGacha = new GachaDebugData { Type = GachaType.Drone, Level = 1, Count = 0 };

        /// <summary>
        /// 특정 가챠 타입의 디버그 데이터를 반환합니다
        /// </summary>
        public GachaDebugData GetGachaData(GachaType type)
        {
            return type switch
            {
                GachaType.Equipment => EquipmentGacha,
                GachaType.Drone => DroneGacha,
                _ => null
            };
        }

        /// <summary>
        /// 특정 가챠 타입의 레벨을 반환합니다
        /// </summary>
        public int GetLevel(GachaType type)
        {
            var data = GetGachaData(type);
            return data != null ? data.Level : 1;
        }

        /// <summary>
        /// 특정 가챠 타입의 카운트를 반환합니다
        /// </summary>
        public int GetCount(GachaType type)
        {
            var data = GetGachaData(type);
            return data != null ? data.Count : 0;
        }
    }
}