using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class FocusGlowAnimation : MonoBehaviour
    {
        [Header("Glow 이미지 설정")]
        [SerializeField] private Image _glowImage;

        [Header("애니메이션 설정")]
        [SerializeField, Tooltip("페이드 최소 알파 값")]
        private float _minAlpha = 0.3f;

        [SerializeField, Tooltip("페이드 최대 알파 값")]
        private float _maxAlpha = 1.0f;

        [SerializeField, Tooltip("애니메이션 한 사이클 시간 (초)")]
        private float _animationDuration = 0.5f;

        private Color _baseColor;
        private bool _isAnimating;

        private void Awake()
        {
            if (_glowImage == null)
                _glowImage = GetComponentInChildren<Image>();

            if (_glowImage != null)
            {
                Color baseColor = _glowImage.color;
                _baseColor = new Color(baseColor.r, baseColor.g, baseColor.b, _maxAlpha);
            }

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _isAnimating = true;
        }

        private void OnDisable()
        {
            _isAnimating = false;

            // 원래 알파 값으로 복원
            if (_glowImage != null)
            {
                _glowImage.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _maxAlpha);
            }
        }

        private void Update()
        {
            if (!_isAnimating || _glowImage == null)
                return;

            // 전역 시간 사용 (Time.time을 사용하여 모든 Focus가 동기화됨)
            float normalizedTime = (Time.time % _animationDuration) / _animationDuration;

            // 사인파를 사용하여 부드러운 페이드 인/아웃
            float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, Mathf.Sin(normalizedTime * Mathf.PI));

            // 알파 값만 변경하고 RGB는 유지
            _glowImage.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
        }

        /// <summary>
        /// 배경 색상과 동일하게 설정합니다
        /// </summary>
        public void SetColor(Color color)
        {
            _baseColor = color;
            if (_glowImage != null)
            {
                // 현재 알파 값은 유지
                float currentAlpha = _glowImage.color.a;
                _glowImage.color = new Color(color.r, color.g, color.b, currentAlpha);
            }
        }

        /// <summary>
        /// 애니메이션을 활성화/비활성화합니다
        /// </summary>
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}