using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SahurRaising.Core
{
    public interface ISkillService
    {
        UniTask InitializeAsync();
        UniTask SaveAsync();

        /// <summary>
        /// 스킬이 이미 해금되었는지 확인
        /// </summary>
        bool IsUnlocked(string skillId);

        /// <summary>
        /// 스킬을 해금할 수 있는지 확인 (비용, 선행 스킬 조건 등)
        /// </summary>
        bool CanUnlock(string skillId);

        /// <summary>
        /// 스킬 해금 시도
        /// </summary>
        bool TryUnlock(string skillId);

        /// <summary>
        /// 전체 스킬 테이블 데이터 조회
        /// </summary>
        SkillTable GetTable();

        /// <summary>
        /// 특수 스킬(Special Effect)의 합산 값을 조회
        /// </summary>
        double GetSpecialValue(SkillSpecialType type);

        /// <summary>
        /// 스킬의 현재 상태 조회 (Locked, Unlockable, Researching, Unlocked)
        /// </summary>
        SkillState GetSkillState(string skillId);

        /// <summary>
        /// 연구 중인 스킬의 남은 시간(초) 조회
        /// </summary>
        double GetRemainingTime(string skillId);

        /// <summary>
        /// 연구 완료 여부 체크 및 처리
        /// </summary>
        void CheckResearchCompletion();

        /// <summary>
        /// 스킬이 새로 해금되었는지 확인 (NEW 태그용)
        /// </summary>
        bool IsNewSkill(string skillId);

        /// <summary>
        /// 스킬 확인 처리 (NEW 태그 제거)
        /// </summary>
        void AcknowledgeSkill(string skillId);

        /// <summary>
        /// 특정 스킬 해금 상태 강제 설정 (디버그용)
        /// </summary>
        void ForceUnlock(string skillId);

        /// <summary>
        /// 특정 스킬 해금 상태 제거 (디버그용)
        /// </summary>
        void ForceLock(string skillId);

        /// <summary>
        /// 모든 스킬 데이터 초기화 (디버그용)
        /// </summary>
        void ResetAllSkills();

        /// <summary>
        /// 모든 해금된 스킬 ID 목록 조회 (디버그용)
        /// </summary>
        IReadOnlyCollection<string> GetUnlockedSkillIds();
    }
}
