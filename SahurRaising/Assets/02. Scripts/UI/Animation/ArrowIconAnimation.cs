using DG.Tweening;
using UnityEngine;

namespace SahurRaising
{
    [RequireComponent(typeof(RectTransform))]
    public class ArrowIconAnimation : MonoBehaviour
    {
        [Header("애니메이션 설정")]
        [SerializeField, Tooltip("위아래 이동 거리 (픽셀)")]
        private float _moveDistance = 5f;

        [SerializeField, Tooltip("애니메이션 한 사이클 시간 (초)")]
        private float _animationDuration = 1.0f;

        [SerializeField, Tooltip("크기 펄스 배율 (1.0 = 원래 크기)")]
        private float _scaleMultiplier = 1.15f;

        private RectTransform _rectTransform;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _isAnimating;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.anchoredPosition;
                _originalScale = _rectTransform.localScale;
            }

            // 초기에는 비활성화 상태
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _isAnimating = true;
        }

        private void OnDisable()
        {
            _isAnimating = false;

            // 원래 위치로 복원
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originalPosition;
                _rectTransform.localScale = _originalScale;
            }
        }

        private void Update()
        {
            if (!_isAnimating || _rectTransform == null)
                return;

            // 전역 시간 사용 (Time.time을 사용하여 모든 화살표가 동기화됨)
            float normalizedTime = (Time.time % _animationDuration) / _animationDuration;

            // 위아래 이동 (0~0.5: 위로, 0.5~1.0: 아래로)
            float yOffset;
            if (normalizedTime < 0.5f)
            {
                // 위로 이동 (0 -> 1)
                float t = normalizedTime * 2f;
                yOffset = Mathf.Sin(t * Mathf.PI) * _moveDistance;
            }
            else
            {
                // 아래로 이동 (1 -> 0)
                float t = (normalizedTime - 0.5f) * 2f;
                yOffset = Mathf.Sin((1f - t) * Mathf.PI) * _moveDistance;
            }

            // 크기 펄스 (0~0.5: 커짐, 0.5~1.0: 작아짐)
            float scale;
            if (normalizedTime < 0.5f)
            {
                float t = normalizedTime * 2f;
                scale = Mathf.Lerp(1f, _scaleMultiplier, Mathf.Sin(t * Mathf.PI));
            }
            else
            {
                float t = (normalizedTime - 0.5f) * 2f;
                scale = Mathf.Lerp(_scaleMultiplier, 1f, Mathf.Sin(t * Mathf.PI));
            }

            // 위치와 크기 적용
            _rectTransform.anchoredPosition = new Vector2(
                _originalPosition.x,
                _originalPosition.y + yOffset);
            _rectTransform.localScale = _originalScale * scale;
        }

        /// <summary>
        /// 업그레이드 가능 여부에 따라 아이콘을 활성화/비활성화합니다.
        /// </summary>
        public void SetUpgradeAvailable(bool isAvailable)
        {
            gameObject.SetActive(isAvailable);
        }
    }
}
