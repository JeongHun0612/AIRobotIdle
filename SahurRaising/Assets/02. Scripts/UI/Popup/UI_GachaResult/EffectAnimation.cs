using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class EffectAnimation : MonoBehaviour
    {
        [Header("애니메이션 설정")]
        [SerializeField, Tooltip("애니메이션 지속 시간 (초)")]
        private float _animationDuration = 0.3f;

        [SerializeField, Tooltip("회전 각도 (시계방향: 음수, 반시계방향: 양수)")]
        private float _rotationAngle = -360f;

        [SerializeField, Tooltip("회전 Ease 타입")]
        private Ease _rotationEase = Ease.Linear;

        [SerializeField, Tooltip("크기 축소 Ease 타입")]
        private Ease _scaleEase = Ease.InBack;

        private RectTransform _rectTransform;
        private Image _image;
        private Tween _effectTween;

        private Vector3 _baseScale;
        private float _baseAlpha;

        private void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            
            if (_image == null)
                _image = GetComponent<Image>();

            if (_rectTransform != null)
            {
                _baseScale = _rectTransform.localScale;
            }

            if (_image != null)
            {
                _baseAlpha = _image.color.a;
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void OnDestroy()
        {
            StopAnimation();
        }

        /// <summary>
        /// Effect 색상을 설정합니다
        /// </summary>
        public void SetColor(Color color)
        {
            if (_image == null)
                _image = GetComponent<Image>();

            _image.color = new Color(color.r, color.g, color.b, _baseAlpha);
        }

        /// <summary>
        /// Effect 연출을 시작합니다 (z축 회전 + 크기 축소)
        /// </summary>
        public void StartAnimation()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            StopAnimation();

            // Effect 오브젝트 활성화
            gameObject.SetActive(true);

            // 초기 상태 설정
            _rectTransform.localScale = Vector3.one;
            _rectTransform.localRotation = Quaternion.identity;

            // z축 회전과 크기 축소를 동시에 실행
            Sequence sequence = DOTween.Sequence();

            // z축 회전 (360도)
            sequence.Append(_rectTransform.DORotate(new Vector3(0, 0, _rotationAngle), _animationDuration, RotateMode.FastBeyond360)
                .SetEase(_rotationEase));

            // 크기 축소
            sequence.Join(_rectTransform.DOScale(Vector3.zero, _animationDuration)
                .SetEase(_scaleEase));

            sequence.OnComplete(() =>
            {
                if (_rectTransform != null)
                {
                    gameObject.SetActive(false);
                }
            });

            _effectTween = sequence;
        }

        /// <summary>
        /// Effect 연출을 중지합니다
        /// </summary>
        public void StopAnimation()
        {
            _effectTween?.Kill();
            _effectTween = null;

            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = _baseScale;
                _rectTransform.localRotation = Quaternion.identity;
            }

            gameObject.SetActive(false);
        }
    }
}