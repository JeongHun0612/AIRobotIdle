using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 플레이어 유닛 전용 뷰
    /// - Idle, Move, Attack, Dead 애니메이션 처리
    /// </summary>
    public class PlayerUnitView : UnitView
    {
        // 플레이어 전용 애니메이션 해시
        private static readonly int AnimStateIdle = Animator.StringToHash("Idle");
        private static readonly int AnimStateMove = Animator.StringToHash("Move");
        private static readonly int AnimStateAttack = Animator.StringToHash("Attack");
        private static readonly int AnimStateDead = Animator.StringToHash("Die");

        // 부모 클래스의 프로퍼티 오버라이드
        // 참고: MoveAnimHash는 이동 중일 때 사용되며, Idle은 별도로 처리
        protected override int MoveAnimHash => AnimStateMove;
        protected override int AttackAnimHash => AnimStateAttack;
        protected override int DeadAnimHash => AnimStateDead;

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_currentState == UnitState.Dead) return;

            // 플레이어는 항상 몬스터보다 앞에 보이도록 Z값을 더 앞으로 당김
            // UnitView의 기본 baseZ가 -5.0f이므로, 플레이어는 -6.0f 정도로 설정하여 무조건 앞에 오게 함
            var pos = transform.position;
            pos.z = _baseZ - 1.0f;
            transform.position = pos;
        }

        /// <summary>
        /// 이동/대기 상태 전환 오버라이드
        /// - isMoving이 true면 Move 애니메이션, false면 Idle 애니메이션
        /// </summary>
        public override void PlayMove(bool isMoving = true)
        {
            if (IsDead) return;

            // 공격 중일 때 PlayMove가 호출되면 예약만 해둠
            if (_currentState == UnitState.Attack)
            {
                _isMovingParams = isMoving;
                return;
            }

            _currentState = UnitState.Move;
            _isMovingParams = isMoving;

            if (_animator != null && gameObject.activeInHierarchy)
            {
                int targetAnimHash = isMoving ? AnimStateMove : AnimStateIdle;

                // 이미 해당 애니메이션이 재생 중이면 다시 Play하지 않음
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != targetAnimHash)
                {
                    _animator.Play(targetAnimHash);
                }
            }
        }
    }
}
