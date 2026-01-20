using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// UI 기반 배경 스크롤링 컴포넌트
    /// Canvas 안의 Image/RawImage를 스크롤합니다.
    /// </summary>
    public class UIBackgroundScroller : MonoBehaviour
    {
        [Header("스크롤 대상")]
        [Tooltip("스크롤할 배경 레이어들")]
        [SerializeField] private UIBackgroundLayer[] _layers;
        
        [Header("설정")]
        [Tooltip("기본 스크롤 속도")]
        [SerializeField] private float _baseScrollSpeed = 100f;  // UI는 픽셀 단위
        
        private bool _isScrolling = false;
        private float _currentSpeed = 0f;
        private float _targetSpeed = 0f;
        private float _accelerationTime = 0.3f;

        public void StartScrolling(float speed = -1f)
        {
            _isScrolling = true;
            _targetSpeed = speed > 0 ? speed : _baseScrollSpeed;
        }

        public void StopScrolling()
        {
            _isScrolling = false;
            _targetSpeed = 0f;
        }

        private void Update()
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime / _accelerationTime);
            
            if (Mathf.Abs(_currentSpeed) < 0.1f)
            {
                _currentSpeed = 0f;
                return;
            }
            
            foreach (var layer in _layers)
            {
                if (layer.ScrollType == UIScrollType.RectTransform)
                {
                    ScrollRectTransform(layer);
                }
                else if (layer.ScrollType == UIScrollType.MaterialOffset)
                {
                    ScrollMaterialOffset(layer);
                }
            }
        }

        /// <summary>
        /// RectTransform 위치를 이동하여 스크롤
        /// </summary>
        private void ScrollRectTransform(UIBackgroundLayer layer)
        {
            if (layer.RectTransform == null) return;
            
            float layerSpeed = _currentSpeed * layer.SpeedMultiplier;
            Vector2 pos = layer.RectTransform.anchoredPosition;
            pos.x -= layerSpeed * Time.deltaTime;
            
            // 루프 처리
            if (layer.EnableLoop && layer.LoopWidth > 0)
            {
                if (pos.x <= -layer.LoopWidth)
                {
                    pos.x += layer.LoopWidth * 2;
                }
            }
            
            layer.RectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// Material UV Offset으로 스크롤 (RawImage 권장)
        /// 하나의 이미지로 무한 스크롤 가능
        /// </summary>
        private void ScrollMaterialOffset(UIBackgroundLayer layer)
        {
            if (layer.RawImage == null) return;
            
            float layerSpeed = _currentSpeed * layer.SpeedMultiplier * 0.001f; // UV는 0~1 범위
            Rect uvRect = layer.RawImage.uvRect;
            uvRect.x += layerSpeed * Time.deltaTime;
            
            // UV는 자동으로 반복됨 (Wrap Mode가 Repeat인 경우)
            layer.RawImage.uvRect = uvRect;
        }

        public void SetBaseSpeed(float speed)
        {
            _baseScrollSpeed = speed;
            if (_isScrolling) _targetSpeed = speed;
        }

        public bool IsScrolling => _isScrolling;
    }

    public enum UIScrollType
    {
        [Tooltip("RectTransform 위치 이동 (복제본 필요)")]
        RectTransform,
        
        [Tooltip("Material UV Offset 이동 (무한 스크롤, RawImage 사용)")]
        MaterialOffset
    }

    [System.Serializable]
    public class UIBackgroundLayer
    {
        [Tooltip("스크롤 방식")]
        public UIScrollType ScrollType = UIScrollType.MaterialOffset;
        
        [Header("RectTransform 방식")]
        [Tooltip("스크롤할 RectTransform")]
        public RectTransform RectTransform;
        
        [Tooltip("루프 너비 (픽셀)")]
        public float LoopWidth = 1920f;
        
        [Tooltip("무한 루프 활성화")]
        public bool EnableLoop = true;
        
        [Header("MaterialOffset 방식 (권장)")]
        [Tooltip("RawImage 컴포넌트 (Image가 아닌 RawImage 사용)")]
        public RawImage RawImage;
        
        [Header("공통")]
        [Tooltip("속도 배율 (패럴랙스)")]
        [Range(0.1f, 2f)]
        public float SpeedMultiplier = 1f;
    }
}
