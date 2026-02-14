using System;
using UnityEngine;

namespace SahurRaising.UI
{
    /// <summary>
    /// 티어별 아이콘 영역 색상 세트 (배경, 테두리, 하이라이트)
    /// </summary>
    [Serializable]
    public struct TierColorSet
    {
        [Tooltip("아이콘 배경 메인 색상")]
        public Color BaseColor;

        [Tooltip("아이콘 테두리 색상 (배경보다 어둡게 설정 권장)")]
        public Color BorderColor;

        [Tooltip("아이콘 상단 하이라이트 색상 (배경보다 밝게/파스텔 톤 권장)")]
        public Color LightColor;
    }

    [CreateAssetMenu(fileName = "UpgradeTierTheme", menuName = "SahurRaising/UI/UpgradeTierTheme")]
    public class UpgradeTierThemeSO : ScriptableObject
    {
        [Header("티어별 색상 세트")]
        [Tooltip("Index 0: Tier 0 (UP0xx), Index 1: Tier 1 (UP1xx), ...")]
        public TierColorSet[] TierColorSets;

        /// <summary>
        /// 티어 인덱스에 해당하는 색상 세트를 반환합니다.
        /// 범위 밖이면 기본 흰색 세트를 반환합니다.
        /// </summary>
        public TierColorSet GetColorSet(int tierIndex)
        {
            if (TierColorSets == null || TierColorSets.Length == 0)
                return DefaultColorSet;

            if (tierIndex < 0 || tierIndex >= TierColorSets.Length)
                return DefaultColorSet;

            return TierColorSets[tierIndex];
        }

        private static readonly TierColorSet DefaultColorSet = new()
        {
            BaseColor = Color.white,
            BorderColor = new Color(0.7f, 0.7f, 0.7f, 1f),
            LightColor = new Color(1f, 1f, 1f, 0.6f)
        };
    }
}
