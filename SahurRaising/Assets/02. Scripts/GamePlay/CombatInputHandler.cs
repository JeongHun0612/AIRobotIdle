using UnityEngine;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 전투 관련 입력을 처리하는 컴포넌트
    /// CombatRunner에서 입력 로직을 분리
    /// </summary>
    public class CombatInputHandler : MonoBehaviour
    {
        private ICombatService _combatService;
        private bool _isEnabled = false;

        /// <summary>
        /// 입력 핸들러 초기화
        /// </summary>
        public void Initialize(ICombatService combatService)
        {
            _combatService = combatService;
            _isEnabled = true;
        }

        /// <summary>
        /// 입력 처리 활성화/비활성화
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        private void Update()
        {
            if (!_isEnabled || _combatService == null) return;

            HandleTouchInput();
        }

        private void HandleTouchInput()
        {
            // 마우스/터치 입력 감지
            if (!Input.GetMouseButtonDown(0)) return;

            // EventSystem 존재 여부 확인
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return;

            // UI 위에서의 클릭은 무시
            if (eventSystem.IsPointerOverGameObject())
                return;

            // 모바일 터치의 경우 추가 확인
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (eventSystem.IsPointerOverGameObject(touch.fingerId))
                    return;
            }

            // 터치 공격 적용
            _combatService.ApplyTouchAttack();
        }

        private void OnDestroy()
        {
            _combatService = null;
            _isEnabled = false;
        }
    }
}
