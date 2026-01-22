using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

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

        [Header("Sorting Settings")]
        [SerializeField] protected float _baseZ = -5.0f;
        [Tooltip("정렬 기준점 Y 오프셋 (보통 발 위치로 맞춤). 값이 작을수록(음수) 정렬 기준이 아래로 내려감.")]
        [SerializeField] protected float _sortingOffsetY = 0f;
        [Tooltip("Z축 정렬 민감도. 값이 클수록 Y 위치에 따른 Z 변화가 커짐.")]
        [SerializeField] protected float _zSortingMultiplier = 0.1f;

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
            // 3D 메쉬(SkinnedMeshRenderer) 사용 시 SortingOrder보다 Z축(Depth) 정렬이 확실함
            // 사용자 요청에 따라 Layer/SortingOrder 방식 대신 Z축 조절 방식으로 회귀
            
            var pos = transform.position;
            
            // 정렬 기준 Y값 계산 (발 위치 기준 오프셋 적용)
            float sortingY = pos.y + _sortingOffsetY;

            // Z축 정렬 (Physical Depth)
            // Y가 낮을수록(아래쪽) -> 카메라에 가까워야 함 -> Z값이 작아져야 함 (Camera가 -Z 방향에 있다고 가정)
            // Y가 높을수록(위쪽) -> 카메라에서 멀어져야 함 -> Z값이 커져야 함
            
            // 기본 Z 위치에서 Y값에 비례하여 Z를 더함
            // 예: Y가 크면(위) Z도 커짐(뒤로 감). Y가 작으면(아래) Z도 작아짐(앞으로 옴).
            pos.z = _baseZ + (sortingY * _zSortingMultiplier);
            
            transform.position = pos;
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
                // 이미 해당 애니메이션이 재생 중이면 다시 Play하지 않음
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != MoveAnimHash)
                {
                    _animator.Play(MoveAnimHash);
                }
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
        /// <summary>
        /// 에디터에서 정렬 기준점을 시각적으로 확인하기 위한 기즈모
        /// </summary>
        /// <summary>
        /// 에디터에서 정렬 기준점을 시각적으로 확인하기 위한 기즈모
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            // 정렬 기준 Y 계산
            float sortingY = transform.position.y + _sortingOffsetY;

            // 기즈모 그리기
            Gizmos.color = Color.red;
            Vector3 pivotPos = transform.position;
            pivotPos.y = sortingY;
            pivotPos.z = _baseZ + (sortingY * _zSortingMultiplier); // 예상 Z 위치

            // 기준점 표시 (빨간 공)
            Gizmos.DrawSphere(pivotPos, 0.1f);
            
            // 기준선 표시 (가로 선)
            Gizmos.DrawLine(pivotPos + Vector3.left * 0.5f, pivotPos + Vector3.right * 0.5f);
        }
    }
}
