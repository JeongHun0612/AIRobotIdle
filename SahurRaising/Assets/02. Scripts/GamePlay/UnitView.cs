using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 유닛(플레이어/몬스터)의 공통 비주얼 및 애니메이션 제어 (Base Class)
    /// </summary>
    public abstract class UnitView : MonoBehaviour
    {
        public enum UnitState
        {
            Move,   // 기본 상태 (이동 중 or 대기 중)
            Attack, // 공격 중
            Dead    // 사망
        }

        [Header("Base Components")]
        [SerializeField] protected Animator _animator;

        [Header("Base Settings")]
        [HideInInspector]
        [SerializeField] protected float _attackAnimDuration = 0.4f;

        // 상태 관리
        protected UnitState _currentState;
        protected bool _isMovingParams; // 실제 이동 중인지 여부

        // 애니메이터 해시 프로퍼티 (자식 클래스에서 반드시 구현)
        protected abstract int MoveAnimHash { get; }
        protected abstract int AttackAnimHash { get; }
        protected abstract int DeadAnimHash { get; }

        public bool IsDead => _currentState == UnitState.Dead;
        public bool IsAttacking => _currentState == UnitState.Attack;

        public virtual void Initialize()
        {
            gameObject.SetActive(true);
            
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            // 초기 상태는 Move (기본)
            PlayMove(true);
        }

        protected virtual void Start()
        {
            // 안전장치: 외부 초기화가 없었다면 스스로 초기화
            if (_animator == null)
            {
                Initialize();
            }
        }

        protected virtual void LateUpdate()
        {
            // Base에서는 특별한 업데이트 로직 없음
        }

        /// <summary>
        /// 이동(기본) 상태로 전환
        /// </summary>
        /// <param name="isMoving">true면 이동 애니메이션/로직 수행, false면 대기</param>
        public virtual void PlayMove(bool isMoving = true)
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
                _animator.Play(MoveAnimHash);
            }
        }

        /// <summary>
        /// 공격 상태로 전환
        /// </summary>
        public virtual void PlayAttack()
        {
            if (IsDead) return;

            _currentState = UnitState.Attack;
            _isMovingParams = false; // 공격 중엔 이동 멈춤

            if (_animator != null && gameObject.activeInHierarchy)
            {
                _animator.Play(AttackAnimHash, -1, 0f);
                WaitForAttackEndAsync().Forget();
            }
        }

        protected async UniTaskVoid WaitForAttackEndAsync()
        {
            // 애니메이터 상태 전환 대기
            await UniTask.Yield(PlayerLoopTiming.Update);

            float duration = _attackAnimDuration;

            if (_animator != null)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                
                if (stateInfo.shortNameHash == AttackAnimHash)
                {
                    duration = stateInfo.length;
                }
            }

            await UniTask.Delay(System.TimeSpan.FromSeconds(duration));

            // 복귀 로직
            if (!IsDead && _currentState == UnitState.Attack)
            {
                _currentState = UnitState.Move;
                PlayMove(_isMovingParams); // 예약된 상태로 복귀
            }
        }

        /// <summary>
        /// 사망 상태로 전환
        /// </summary>
        public virtual void PlayDie()
        {
            _currentState = UnitState.Dead;
            _isMovingParams = false;

            if (_animator != null && gameObject.activeInHierarchy)
            {
                _animator.Play(DeadAnimHash);
            }
        }

        /// <summary>
        /// 캐릭터 좌우 반전
        /// </summary>
        public void Flip(bool isLeft)
        {
            Vector3 scale = transform.localScale;
            scale.x = isLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
