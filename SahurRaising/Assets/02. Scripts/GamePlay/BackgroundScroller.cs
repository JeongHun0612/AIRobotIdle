using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 배경 스크롤링을 담당하는 컴포넌트
    /// 이동 중에는 배경이 스크롤되어 플레이어가 이동하는 느낌을 줍니다.
    /// </summary>
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("스크롤 대상")]
        [Tooltip("스크롤할 배경 오브젝트들 (여러 레이어 지원)")]
        [SerializeField] private BackgroundLayer[] _layers;
        
        // [Tooltip("기본 스크롤 속도")]
        // [SerializeField] private float _baseScrollSpeed = 2f; // CombatSettings에서 제어하므로 인스펙터 노출 제거
        private float _baseScrollSpeed = 2f;
        
        private bool _isScrolling = false;
        private float _currentSpeed = 0f;
        private float _targetSpeed = 0f;
        private float _accelerationTime = 0.3f;

        /// <summary>
        /// 스크롤 시작 (플레이어 이동 중)
        /// </summary>
        public void StartScrolling(float speed = -1f)
        {
            _isScrolling = true;
            _targetSpeed = speed > 0 ? speed : _baseScrollSpeed;
        }

        /// <summary>
        /// 스크롤 정지 (전투 중)
        /// </summary>
        public void StopScrolling()
        {
            _isScrolling = false;
            _targetSpeed = 0f;
        }

        private void Update()
        {
            // 부드러운 속도 전환
            _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime / _accelerationTime);
            
            if (Mathf.Abs(_currentSpeed) < 0.01f)
            {
                _currentSpeed = 0f;
                return;
            }
            
            // 각 레이어별로 스크롤 (패럴랙스 효과)
            if (_layers == null) return;
            
            foreach (var layer in _layers)
            {
                if (layer.Transform == null) continue;
                
                float layerSpeed = _currentSpeed * layer.SpeedMultiplier;
                
                // 방향에 따라 이동 (왼쪽으로 스크롤 = 플레이어가 오른쪽으로 이동하는 느낌)
                layer.Transform.position += Vector3.left * layerSpeed * Time.deltaTime;
                
                // 반복 스크롤 처리 (배경이 끝까지 가면 다시 처음으로)
                if (layer.EnableLoop && layer.LoopWidth > 0)
                {
                    Vector3 pos = layer.Transform.position;
                    if (pos.x <= -layer.LoopWidth)
                    {
                        pos.x += layer.LoopWidth * 2;
                        layer.Transform.position = pos;
                    }
                }
            }
        }

        /// <summary>
        /// 외부에서 기본 속도 설정
        /// </summary>
        public void SetBaseSpeed(float speed)
        {
            _baseScrollSpeed = speed;
            if (_isScrolling)
            {
                _targetSpeed = speed;
            }
        }

        public bool IsScrolling => _isScrolling;
    }

    /// <summary>
    /// 배경 레이어 설정
    /// </summary>
    [System.Serializable]
    public class BackgroundLayer
    {
        [Tooltip("배경 오브젝트의 Transform")]
        public Transform Transform;
        
        [Tooltip("속도 배율 (1 = 기본, 0.5 = 절반 속도로 패럴랙스 효과)")]
        [Range(0.1f, 2f)]
        public float SpeedMultiplier = 1f;
        
        [Tooltip("무한 반복 스크롤 활성화")]
        public bool EnableLoop = true;
        
        [Tooltip("반복할 배경의 너비 (루프 포인트)")]
        public float LoopWidth = 20f;
    }
}
