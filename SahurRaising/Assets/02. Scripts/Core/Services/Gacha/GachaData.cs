using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    public enum GachaType
    {
        Equipment,  // 장비 뽑기 (다이아몬드 사용)
        Drone,      // 드론 뽑기 (에메랄드 사용)
        None = 10000,
    }

    [Serializable]
    public struct GachaProbability
    {
        public string GradeKey;
        public float Probability;
    }

    // ========== Equipment Gacha ==========

    [Serializable]
    public class RemoteGachaEquipmentTableData
    {
        public List<GachaEquipmentRow> Rows = new();
    }

    [Serializable]
    public struct GachaEquipmentRow
    {
        public int Level;
        public List<EquipmentProbability> Probabilities;
    }

    [Serializable]
    public struct EquipmentProbability
    {
        public string Grade;
        public float Probability;

        /// <summary>
        /// Grade 문자열을 enum으로 변환
        /// </summary>
        public EquipmentGrade GetGradeEnum()
        {
            if (System.Enum.TryParse<EquipmentGrade>(Grade, true, out var grade))
                return grade;

            Debug.LogWarning($"[EquipmentProbability] 알 수 없는 등급: {Grade}");
            return EquipmentGrade.F; // 기본값
        }
    }

    // ========== Drone Gacha ==========

    [Serializable]
    public class RemoteGachaDroneTableData
    {
        public List<GachaDroneRow> Rows = new();
    }

    [Serializable]
    public struct GachaDroneRow
    {
        public int Level;
        public List<DroneProbability> Probabilities;
    }

    [Serializable]
    public struct DroneProbability
    {
        public string ID;
        public float Probability;
    }

    // ========== Save Data ==========

    [Serializable]
    public class GachaSaveData
    {
        public List<GachaTypeSaveData> GachaDataList = new();
    }

    [Serializable]
    public struct GachaTypeSaveData
    {
        public GachaType Type;
        public int Count;
        public int Level;

        public GachaTypeSaveData(GachaType type, int count, int level)
        {
            Type = type;
            Count = count;
            Level = level;
        }
    }
}