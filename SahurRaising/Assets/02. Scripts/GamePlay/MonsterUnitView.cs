using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 몬스터 유닛 전용 뷰
    /// - 현재는 기본 UnitView 기능만 사용하지만, 추후 몬스터 전용 이펙트/애니메이션 확장을 위해 분리
    /// </summary>
    public class MonsterUnitView : UnitView
    {
        // 몬스터 전용 애니메이션 해시
        private static readonly int AnimStateWalk = Animator.StringToHash("Walk");
        private static readonly int AnimStateRun = Animator.StringToHash("Run"); // 추후 속도에 따라 사용 예정
        private static readonly int AnimStateAttackMonster = Animator.StringToHash("Attack");
        private static readonly int AnimStateDeath = Animator.StringToHash("Death");

        // 부모 클래스의 프로퍼티 오버라이드
        protected override int MoveAnimHash => AnimStateWalk; // 기본은 Walk, 필요 시 Run으로 분기 가능
        protected override int AttackAnimHash => AnimStateAttackMonster;
        protected override int DeadAnimHash => AnimStateDeath;
    }
}
