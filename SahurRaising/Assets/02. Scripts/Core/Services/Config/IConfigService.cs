using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 공통으로 사용되는 Config(ScriptableObject) 접근을 제공하는 서비스.
    /// </summary>
    public interface IConfigService
    {
        UniTask InitializeAsync();
        bool IsInitialized { get; }

        // 아이템(장비/드론) 시각 설정
        ItemVisualConfig ItemVisualConfig { get; }

        // 스킬 시각 설정
        SkillVisualConfig SkillVisualConfig { get; }

        Color GetColorForGrade(GachaType gachaType, string gradeKey);
        Sprite GetTypeIcon(GachaType gachaType, string typeKey);

        /// <summary>
        /// 스킬 카테고리(Prefix)에 해당하는 프레임 색상 반환
        /// </summary>
        Color GetSkillFrameColor(SkillIdPrefix prefix);

        /// <summary>
        /// 스킬 ID 또는 Prefix 문자열로 프레임 색상 반환
        /// </summary>
        Color GetSkillFrameColor(string skillIdOrPrefix);
    }
}

