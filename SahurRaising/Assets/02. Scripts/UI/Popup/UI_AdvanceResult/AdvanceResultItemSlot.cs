using SahurRaising.Core;
using SahurRaising.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class AdvanceResultItemSlot : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _gradeText;
        [SerializeField] private TMP_Text _countText;

        [Header("잼 아이콘")]
        [SerializeField] private GameObject[] _gemIcons;

        public void UpdateUI(AdvanceResult result, IConfigService configService, IGachaResultStrategy strategy)
        {
            if (string.IsNullOrEmpty(result.ItemCode))
                return;

            if (configService == null || configService.ItemVisualConfig == null)
            {
                Debug.LogWarning("[AdvanceResultItemSlot] ItemVisualConfig를 찾을 수 없습니다.");
                return;
            }

            // 배경 색깔 설정
            if (_bgImage != null)
            {
                _bgImage.color = configService.GetColorForGrade(result.Type, result.GradeKey);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                var icon = result.Icon;
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }

            // 등급 텍스트 설정
            UpdateGradeText(result.GradeKey, strategy);

            // 수량 텍스트 설정
            if (_countText != null)
            {
                _countText.text = result.Count.ToString();
            }

            // 잼 아이콘 설정
            UpdateGemIcons(result.GradeKey, strategy);
        }

        private void UpdateGradeText(string gradeKey, IGachaResultStrategy strategy)
        {
            if (_gradeText == null || string.IsNullOrEmpty(gradeKey))
                return;

            if (strategy != null)
            {
                _gradeText.text = strategy.FormatGradeText(gradeKey);
            }
            else
            {
                _gradeText.text = gradeKey;
            }
        }

        private void UpdateGemIcons(string gradeKey, IGachaResultStrategy strategy)
        {
            if (_gemIcons == null || _gemIcons.Length == 0 || string.IsNullOrEmpty(gradeKey))
                return;

            int gemCount = 0;
            if (strategy != null)
            {
                gemCount = strategy.GetGemCount(gradeKey);
            }

            if (gemCount > 0)
                gemCount = 4 - gemCount;

            for (int i = 0; i < _gemIcons.Length; i++)
            {
                if (_gemIcons[i] != null)
                {
                    _gemIcons[i].SetActive(i < gemCount);
                }
            }
        }
    }
}
