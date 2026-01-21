using UnityEngine;
using BreakInfinity;
using Cysharp.Threading.Tasks;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 몬스터 유닛 전용 뷰
    /// - 개별 HP 관리
    /// - 간격 대기 시 Idle-A 애니메이션
    /// - 사망 후 풀 반환 처리
    /// </summary>
    public class MonsterUnitView : UnitView
    {
        // 몬스터 전용 애니메이션 해시
        private static readonly int AnimStateWalk = Animator.StringToHash("Walk");
        private static readonly int AnimStateIdleA = Animator.StringToHash("Idle-A");
        private static readonly int AnimStateAttackMonster = Animator.StringToHash("Attack");
        private static readonly int AnimStateDeath = Animator.StringToHash("Death");

        // 부모 클래스의 프로퍼티 오버라이드
        protected override int MoveAnimHash => AnimStateWalk;
        protected override int AttackAnimHash => AnimStateAttackMonster;
        protected override int DeadAnimHash => AnimStateDeath;

        [Header("몬스터 런타임 데이터")]
        [SerializeField] private float _deathAnimDuration = 0.5f;

        // 런타임 HP 관리
        private BigDouble _maxHp;
        private BigDouble _currentHp;
        private BigDouble _defense;
        private bool _isWaitingForSpace; // 간격 대기 중 여부
        private int _monsterLevel;
        private Core.MonsterKind _monsterKind;

        /// <summary>현재 HP</summary>
        public BigDouble CurrentHp => _currentHp;
        
        /// <summary>최대 HP</summary>
        public BigDouble MaxHp => _maxHp;
        
        /// <summary>방어력</summary>
        public BigDouble Defense => _defense;
        
        /// <summary>몬스터 레벨</summary>
        public int MonsterLevel => _monsterLevel;
        
        /// <summary>몬스터 종류</summary>
        public Core.MonsterKind MonsterKind => _monsterKind;
        
        /// <summary>간격 대기 중인지 여부</summary>
        public bool IsWaitingForSpace => _isWaitingForSpace;

        /// <summary>
        /// 몬스터 스탯 초기화 (스폰 시 호출)
        /// </summary>
        public void SetupStats(BigDouble maxHp, BigDouble defense, int level, Core.MonsterKind kind)
        {
            _maxHp = maxHp;
            _currentHp = maxHp;
            _defense = defense;
            _monsterLevel = level;
            _monsterKind = kind;
            _isWaitingForSpace = false;
        }

        public override void Initialize()
        {
            base.Initialize();
            _isWaitingForSpace = false;
        }

        /// <summary>
        /// 데미지 적용 (방어력 고려)
        /// </summary>
        /// <param name="damage">적용할 데미지</param>
        /// <param name="defenseIgnoreRate">방어 무시 비율 (0~1)</param>
        /// <returns>실제 적용된 데미지</returns>
        public BigDouble TakeDamage(BigDouble damage, double defenseIgnoreRate = 0)
        {
            if (IsDead) return BigDouble.Zero;

            // 방어력 계산
            var effectiveDefense = _defense * (1 - Mathf.Clamp01((float)defenseIgnoreRate));
            var actualDamage = BigDouble.Max(BigDouble.One, damage - effectiveDefense);
            
            _currentHp -= actualDamage;
            
            if (_currentHp <= BigDouble.Zero)
            {
                _currentHp = BigDouble.Zero;
            }
            
            return actualDamage;
        }

        /// <summary>
        /// 간격 대기 상태 설정 (Idle-A 애니메이션)
        /// </summary>
        public void SetWaitingForSpace(bool waiting)
        {
            if (IsDead) return;
            
            _isWaitingForSpace = waiting;
            
            if (waiting && _animator != null && gameObject.activeInHierarchy)
            {
                _animator.Play(AnimStateIdleA);
            }
        }

        /// <summary>
        /// 사망 처리 (애니메이션 후 비활성화)
        /// </summary>
        public override void PlayDie()
        {
            base.PlayDie();
            _isWaitingForSpace = false;
            
            // 사망 애니메이션 후 비활성화
            DeactivateAfterDeathAsync().Forget();
        }

        private async UniTaskVoid DeactivateAfterDeathAsync()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_deathAnimDuration));
            
            // 풀로 반환 준비 (gameObject 비활성화)
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 풀 반환 시 상태 리셋
        /// </summary>
        public void ResetForPool()
        {
            _currentHp = BigDouble.Zero;
            _maxHp = BigDouble.Zero;
            _defense = BigDouble.Zero;
            _monsterLevel = 0;
            _monsterKind = Core.MonsterKind.Normal;
            _isWaitingForSpace = false;
            _currentState = UnitState.Move;
        }
    }
}
