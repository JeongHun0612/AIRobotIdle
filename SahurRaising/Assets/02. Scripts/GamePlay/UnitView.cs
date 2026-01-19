using UnityEngine;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    public class UnitView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Animator _animator;
        
        private SpriteRenderer[] _renderers;

        // 애니메이션 State 이름 해시값 캐싱
        private static readonly int AnimStateIdle = Animator.StringToHash("idle");
        private static readonly int AnimStateAttack = Animator.StringToHash("attack");
        private static readonly int AnimStateHit = Animator.StringToHash("hit");
        private static readonly int AnimStateDead = Animator.StringToHash("dead");

        public void Initialize()
        {
            gameObject.SetActive(true);
            
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            
            // 자식 오브젝트들에 있는 모든 스프라이트 렌더러를 찾아서 캐싱 (피격 효과 등을 위해)
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            
            PlayIdle();
        }

        public void PlayIdle()
        {
            _animator?.Play(AnimStateIdle);
        }

        public void PlayAttack()
        {
            Debug.Log($"[UnitView] PlayAttack Called on {gameObject.name}");
            _animator?.Play(AnimStateAttack, -1, 0f);
        }

        public void PlayHit()
        {
            _animator?.Play(AnimStateHit, -1, 0f);
            // 필요하다면 _renderers를 순회하며 색상 변경 로직 추가 가능
        }

        public void PlayDie()
        {
            _animator?.Play(AnimStateDead);
        }

        public void SetMove(bool isMoving)
        {
            if (isMoving)
            {
                if (_animator == null) return;
                
                // 이미 Idle이면 다시 재생하지 않음 (애니메이션 리셋 방지)
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != AnimStateIdle)
                {
                    _animator.Play(AnimStateIdle);
                }
            }
        }

        public void Flip(bool isLeft)
        {
            // 단일 스프라이트가 아닌, 캐릭터 전체(Transform)를 반전시킵니다.
            // 기본이 오른쪽을 보고 있다고 가정합니다.
            Vector3 scale = transform.localScale;
            scale.x = isLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
