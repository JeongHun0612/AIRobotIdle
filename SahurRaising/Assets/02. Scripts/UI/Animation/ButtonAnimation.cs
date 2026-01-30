using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SahurRaising
{
    /// <summary>
    /// 버튼 애니메이션 컴포넌트
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("애니메이션 설정")]
        [Tooltip("터치 시 스케일 값 (기본: 0.95)")]
        [SerializeField] private float _pressScale = 0.95f;
        
        [Tooltip("터치 다운 애니메이션 시간 (초)")]
        [SerializeField] private float _pressDuration = 0.1f;
        
        [Tooltip("터치 업 애니메이션 시간 (초)")]
        [SerializeField] private float _releaseDuration = 0.4f;

        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private Tween _currentTween;
        private bool _isPressed = false;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            _originalScale = _rectTransform.localScale;
        }

        private void OnEnable()
        {
            // 활성화 시 원래 크기로 리셋
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = _originalScale;
                _isPressed = false;
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 트윈 정리
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
            }
            _isPressed = false;
        }

        private void OnDestroy()
        {
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
                return;

            _isPressed = true;
            PlayPressAnimation();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isPressed)
            {
                _isPressed = false;
                PlayReleaseAnimation();
            }
        }

        private void PlayPressAnimation()
        {
            if (_rectTransform == null)
                return;

            // 기존 트윈 정리
            _rectTransform.DOKill();

            // 작아지는 애니메이션
            _currentTween = _rectTransform.DOScale(_originalScale * _pressScale, _pressDuration)
                .SetEase(Ease.OutQuad);
        }

        private void PlayReleaseAnimation()
        {
            if (_rectTransform == null)
                return;

            // 기존 트윈 정리
            _rectTransform.DOKill();

            // 바운스 효과로 원래 크기로 복귀
            _currentTween = _rectTransform.DOScale(_originalScale, _releaseDuration)
                .SetEase(Ease.OutElastic);
        }

        private bool IsInteractable()
        {
            // Button 컴포넌트가 있으면 interactable 상태 확인
            var button = GetComponent<Button>();
            if (button != null)
            {
                return button.interactable;
            }

            // Button이 없으면 항상 true
            return true;
        }

        /// <summary>
        /// 원래 스케일을 현재 스케일로 업데이트 (런타임에서 스케일 변경 시 호출)
        /// </summary>
        public void UpdateOriginalScale()
        {
            if (_rectTransform != null)
            {
                _originalScale = _rectTransform.localScale;
            }
        }
    }
}