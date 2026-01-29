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
}
