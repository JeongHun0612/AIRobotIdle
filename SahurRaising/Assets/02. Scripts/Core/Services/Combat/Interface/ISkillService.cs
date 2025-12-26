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
    }
}
