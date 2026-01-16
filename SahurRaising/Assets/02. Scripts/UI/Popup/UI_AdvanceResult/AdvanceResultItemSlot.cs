using SahurRaising.Core;
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

        private IConfigService _configService;

        public void UpdateUI(AdvanceResult result)
        {
            if (string.IsNullOrEmpty(result.ItemCode))
                return;

            if (_configService == null)
                _configService = ServiceLocator.Get<IConfigService>();

            if (_configService == null || _configService.ItemVisualConfig == null)
            {
                Debug.LogWarning("[GachaSlot] ItemVisualConfig를 찾을 수 없습니다.");
                return;
            }

            // 배경 색깔 설정
            if (_bgImage != null)
            {
                _bgImage.color = _configService.GetColorForGrade(result.Type, result.GradeKey);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                var icon = result.Icon;
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }

            // 등급 텍스트 설정
            if (_gradeText != null)
            {
                _gradeText.text = result.GradeKey;
            }

            // 수량 텍스트 설정
            if (_countText != null)
            {
                _countText.text = result.Count.ToString();
            }
        }
    }
}
