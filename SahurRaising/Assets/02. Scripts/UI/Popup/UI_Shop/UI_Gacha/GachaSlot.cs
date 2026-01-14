using DG.Tweening;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaSlot : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _typeIcon;
        [SerializeField] private TMP_Text _gradeText;

        private GachaResult _result;

        private IConfigService _configService;

        public void SetData(GachaResult result)
        {
            _result = result;
            UpdateUI();
        }

        /// <summary>
        /// 슬롯을 초기 상태로 리셋합니다
        /// </summary>
        public void OnReset()
        {
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// 슬롯을 최종 상태로 즉시 설정합니다 (애니메이션 스킵)
        /// </summary>
        public void SetFinalState()
        {
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 스케일 애니메이션으로 슬롯을 표시합니다
        /// </summary>
        public Tween ShowWithAnimation(float delay = 0f, float duration = 0.3f)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return null;

            // 기존 트윈이 있으면 정리
            _rectTransform.DOKill();

            // 초기 스케일을 0으로 설정
            _rectTransform.localScale = Vector3.zero;

            // 딜레이 후 스케일 애니메이션 (0 -> 1)
            return _rectTransform.DOScale(Vector3.one, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack);
        }

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(_result.ItemCode))
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
                _bgImage.color = _configService.GetColorForGrade(_result.Type, _result.GradeKey);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                var icon = _result.Icon;
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }

            // 등급 텍스트 설정
            if (_gradeText != null)
            {
                _gradeText.text = _result.GradeKey;
            }

            // 타입 아이콘 설정
            if (_typeIcon != null)
            {
                var typeIconSprite = _configService.GetTypeIcon(_result.Type, _result.TypeKey);

                _typeIcon.sprite = typeIconSprite;
                _typeIcon.gameObject.SetActive(typeIconSprite != null);
            }
        }
    }
}
