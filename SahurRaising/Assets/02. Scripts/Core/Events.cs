using System;
using System.Collections.Generic;
using SahurRaising;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 스킬 상태 변경 이벤트 (해금, 연구 시작, 연구 완료 등)
    /// </summary>
    public struct SkillStateChangedEvent
    {
        public string SkillId;
        public SkillState NewState;

        public SkillStateChangedEvent(string skillId, SkillState newState)
        {
            SkillId = skillId;
            NewState = newState;
        }
    }

    #region Equipment (장비)
    /// <summary>
    /// 장비 인벤토리 변경 이벤트 (추가, 제거, 레벨 변경 등)
    /// </summary>
    public struct EquipmentInventoryChangedEvent
    {
        public EquipmentType EquipmentType;
        public string EquipmentCode;

        public EquipmentInventoryChangedEvent(EquipmentType equipmentType, string equipmentCode)
        {
            EquipmentType = equipmentType;
            EquipmentCode = equipmentCode;
        }
    }

    /// <summary>
    /// 장비 장착/해제 이벤트
    /// </summary>
    public struct EquipmentEquippedEvent
    {
        public EquipmentType EquipmentType;
        public string EquipmentCode; // 빈 문자열이면 해제

        public EquipmentEquippedEvent(EquipmentType equipmentType, string equipmentCode)
        {
            EquipmentType = equipmentType;
            EquipmentCode = equipmentCode ?? string.Empty;
        }
    }
    #endregion
}
