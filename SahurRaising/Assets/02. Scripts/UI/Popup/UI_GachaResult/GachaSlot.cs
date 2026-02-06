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

        [Header("잼 아이콘")]
        [SerializeField] private GameObject[] _gemIcons;

        [Header("연출 오브젝트")]
        [SerializeField] private FocusGlowAnimation _focusGlowAnimation;
        [SerializeField] private EffectAnimation _effectAnimation;

        [Header("등장 연출 설정")]
        [SerializeField] private float _showAnimationDuration = 0.2f;
        [SerializeField] private Ease _showAnimationEase = Ease.OutBack;

        private GachaResult _result;

        private IGachaResultStrategy _strategy;
        private IConfigService _configService;
        private IGachaService _gachaService;

        private Tween _showTween;

        public bool IsHighGradeNewItemSlot => IsHighGradeNewItem();

        private void OnDisable()
        {
            _showTween?.Kill();
        }

        private void OnDestroy()
        {
            _showTween?.Kill();
        }

        public void SetData(GachaResult result)
        {
            if (!TryBindService())
                return;

            _result = result;

            // 전략 가져오기
            _strategy = _gachaService.GetResultStrategy(result.Type);

            if (_strategy == null)
            {
                Debug.LogWarning($"[GachaSlot] {result.Type}에 대한 전략을 찾을 수 없습니다.");
                return;
            }

            UpdateUI();
        }

        /// <summary>
        /// 슬롯을 초기 상태로 리셋합니다
        /// </summary>
        public void OnReset()
        {
            // 모든 트윈 정리
            _showTween?.Kill();

            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.zero;
            }

            // 연출 오브젝트 리셋
            StopFocusEffect();
            StopEffectAnimation();
        }

        /// <summary>
        /// 슬롯을 최종 상태로 즉시 설정합니다 (애니메이션 스킵)
        /// </summary>
        public void SetFinalState()
        {
            // 모든 트윈 정리
            _showTween?.Kill();

            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.one;
            }

            // 연출 오브젝트 리셋
            StopFocusEffect();
            StopEffectAnimation();
        }

        /// <summary>
        /// 스케일 애니메이션으로 슬롯을 표시합니다
        /// </summary>
        public Tween ShowWithAnimation(float delay = 0f)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return null;

            // 기존 트윈이 있으면 정리
            _showTween?.Kill();

            // 초기 스케일을 0으로 설정
            _rectTransform.localScale = Vector3.zero;

            // Effect는 Scale과 동시에 시작
            DOVirtual.DelayedCall(delay, () => StartEffectAnimation());

            // 딜레이 후 스케일 애니메이션 (0 -> 1)
            return _showTween = _rectTransform.DOScale(Vector3.one, _showAnimationDuration)
                .SetDelay(delay)
                .SetEase(_showAnimationEase)
                .OnComplete(() =>
                {
                    StartFocusEffect();
                });
        }

        /// <summary>
        /// Focus 연출을 시작합니다
        /// </summary>
        public void StartFocusEffect()
        {
            if (!IsHighGrade())
                return;

            if (_focusGlowAnimation == null)
                return;

            // 배경 색상과 동일하게 설정
            if (_bgImage != null)
            {
                _focusGlowAnimation.SetColor(_bgImage.color);
            }

            // 애니메이션 활성화
            _focusGlowAnimation.gameObject.SetActive(true);
        }

        /// <summary>
        /// Focus 연출을 중지합니다
        /// </summary>
        public void StopFocusEffect()
        {
            if (_focusGlowAnimation != null)
            {
                _focusGlowAnimation.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Effect 연출을 시작합니다
        /// </summary>
        public void StartEffectAnimation()
        {
            if (_effectAnimation == null)
                return;

            // Effect 색상을 배경 색상과 동일하게 설정
            if (_bgImage != null)
            {
                _effectAnimation.SetColor(_bgImage.color);
            }

            // Effect 연출 시작
            _effectAnimation.StartAnimation();
        }

        /// <summary>
        /// Effect 연출을 중지합니다
        /// </summary>
        public void StopEffectAnimation()
        {
            if (_effectAnimation != null)
            {
                _effectAnimation.StopAnimation();
            }
        }

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(_result.ItemCode))
                return;

            if (_configService == null || _configService.ItemVisualConfig == null)
            {
                Debug.LogWarning("[GachaSlot] ItemVisualConfig를 찾을 수 없습니다.");
                return;
            }

            if (_strategy == null)
            {
                Debug.LogWarning("[GachaSlot] 전략이 설정되지 않았습니다.");
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
                _gradeText.text = _strategy.FormatGradeText(_result.GradeKey);
            }

            // 타입 아이콘 설정
            if (_typeIcon != null)
            {
                var typeIconSprite = _configService.GetTypeIcon(_result.Type, _result.TypeKey);
                _typeIcon.sprite = typeIconSprite;
                _typeIcon.gameObject.SetActive(typeIconSprite != null);
            }

            // 잼 아이콘 설정
            if (_gemIcons != null && _gemIcons.Length > 0)
            {
                int gemCount = _strategy.GetGemCount(_result.GradeKey);
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

        private bool TryBindService()
        {
            if (_configService == null && ServiceLocator.HasService<IConfigService>())
            {
                _configService = ServiceLocator.Get<IConfigService>();
            }

            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            return _configService != null && _gachaService != null;
        }

        /// <summary>
        /// 특정 등급 이상 + 처음 획득한 아이템일 경우
        /// </summary>
        private bool IsHighGradeNewItem()
        {
            if (string.IsNullOrEmpty(_result.ItemCode) || string.IsNullOrEmpty(_result.GradeKey))
                return false;

            if (_strategy == null)
                return false;

            return _strategy.IsHighGrade(_result.GradeKey)
                && _strategy.IsNewItem(_result.ItemCode);
        }

        /// <summary>
        /// 고등급인지 확인합니다
        /// </summary>
        private bool IsHighGrade()
        {
            if (string.IsNullOrEmpty(_result.GradeKey) || _strategy == null)
                return false;

            return _strategy.IsHighGrade(_result.GradeKey);
        }
    }
}
