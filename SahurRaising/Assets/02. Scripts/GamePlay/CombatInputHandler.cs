using UnityEngine;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 전투 관련 입력을 처리하는 컴포넌트
    /// CombatRunner에서 입력 로직을 분리
    /// 
    /// 터치 공격 조건:
    /// - 전투 상태 (적이 공격 범위 내에 있음)
    /// - 쿨타임 충족 (초당 최대 2회)
    /// </summary>
    public class CombatInputHandler : MonoBehaviour
    {
        private ICombatService _combatService;
        private bool _isEnabled = false;
        
        // 현재 전투 중인 몬스터 수 (CombatRunner에서 전달)
        private int _engagedMonsterCount = 0;

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

        /// <summary>
        /// 현재 전투 중인 몬스터 수 업데이트 (CombatRunner에서 호출)
        /// </summary>
        public void UpdateEngagedMonsterCount(int count)
        {
            _engagedMonsterCount = count;
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

            // 쿨타임 및 전투 상태 검사 포함 터치 공격 시도
            // 조건 불충족 시 (쿨타임 미충족, 적 없음 등) 자동으로 무시됨
            _combatService.TryApplyTouchAttack(_engagedMonsterCount);
        }

        private void OnDestroy()
        {
            _combatService = null;
            _isEnabled = false;
        }
    }
}
