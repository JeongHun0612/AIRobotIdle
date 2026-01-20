using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 플레이어 유닛 전용 뷰
    /// - 바퀴 회전 및 흔들림(Wobble/Bounce) 로직 포함
    /// </summary>
    public class PlayerUnitView : UnitView
    {
        [Header("Player Settings")]
        [Tooltip("회전시킬 바퀴 오브젝트들 (없으면 비워두세요)")]
        [SerializeField] private Transform[] _wheels;
        [Tooltip("바퀴 회전 속도 (애니메이션 속도)")]
        [SerializeField] private float _wheelRotateSpeed = 360f;
        [Tooltip("좌우 흔들림 각도 (Wobble)")]
        [SerializeField] private float _wheelWobbleAngle = 10f;
        [Tooltip("위아래 들썩임 높이 (Bounce)")]
        [SerializeField] private float _wheelBounceHeight = 0.1f;

        // 바퀴 애니메이션 상태 관리
        private Quaternion[] _initialWheelRotations;
        private Vector3[] _initialWheelPositions;

        public override void Initialize()
        {
            base.Initialize();

            // 바퀴 초기값 저장
            if (_wheels != null)
            {
                _initialWheelRotations = new Quaternion[_wheels.Length];
                _initialWheelPositions = new Vector3[_wheels.Length];

                for (int i = 0; i < _wheels.Length; i++)
                {
                    if (_wheels[i] != null)
                    {
                        _initialWheelRotations[i] = _wheels[i].localRotation;
                        _initialWheelPositions[i] = _wheels[i].localPosition;
                    }
                }
            }

            if (_wheels == null || _wheels.Length == 0)
            {
                // Debug.LogWarning($"[PlayerUnitView] '{name}'에 바퀴(Wheels)가 할당되지 않았습니다!");
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_currentState == UnitState.Dead) return;

            // 바퀴 회전 처리
            // Move 상태이고, 실제로 이동 중(_isMovingParams)일 때만 회전
            if (_currentState == UnitState.Move && _isMovingParams)
            {
                RotateWheels();
            }
        }

        private void RotateWheels()
        {
            if (_wheels == null || _wheels.Length == 0) return;

            // 반원 바퀴 애니메이션:
            // 1. Wobble: 좌우로 뒤뚱뒤뚱 (Sin)
            // 2. Bounce: 위아래로 들썩들썩 (Abs(Sin))
            
            // 속도 보정: 360도가 1초에 한 바퀴라고 가정하고 주기를 계산
            float frequency = _wheelRotateSpeed / 360f; 
            float time = Time.time * frequency * 2 * Mathf.PI; // 2PI = 1주기

            float wobbleAngle = Mathf.Sin(time) * _wheelWobbleAngle;
            float bounceHeight = Mathf.Abs(Mathf.Sin(time)) * _wheelBounceHeight;

            for (int i = 0; i < _wheels.Length; i++)
            {
                var wheel = _wheels[i];
                if (wheel != null)
                {
                    // 1. 회전 (Wobble)
                    Quaternion baseRot = (_initialWheelRotations != null && i < _initialWheelRotations.Length) 
                        ? _initialWheelRotations[i] 
                        : Quaternion.identity;
                    
                    // Z축 기준으로 좌우 흔들림 적용
                    wheel.localRotation = baseRot * Quaternion.Euler(0, 0, wobbleAngle);

                    // 2. 위치 (Bounce)
                    Vector3 basePos = (_initialWheelPositions != null && i < _initialWheelPositions.Length)
                        ? _initialWheelPositions[i]
                        : Vector3.zero;

                    // Y축으로 들썩임 적용
                    wheel.localPosition = basePos + Vector3.up * bounceHeight;
                }
            }
        }
    }
}
