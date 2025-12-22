using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    public class UI_Popup : UI_Base
    {
        public bool RememberInHistory = true;

        [Header("Settings")]
        [Tooltip("배경 클릭 시 팝업을 닫을지 여부")]
        [SerializeField] private bool _isCloseOnBackdropClick = true;
        
        [Tooltip("배경 버튼 참조 (할당하지 않으면 자동 처리 없음)")]
        [SerializeField] private Button _backdropButton;

        // 하위 클래스에서 오버라이드하여 동적으로 제어 가능하도록 프로퍼티로 제공
        protected virtual bool CanCloseOnBackdropClick => _isCloseOnBackdropClick;

        public override void OnShow()
        {
            base.OnShow();
            UpdateBackdropState();
        }

        private void UpdateBackdropState()
        {
            if (_backdropButton != null)
            {
                // 배경 버튼 활성화/비활성화로 클릭 이벤트 제어
                // (Button 컴포넌트만 끄면 RaycastTarget은 유지되어 뒷 배경 터치는 막힘)
                _backdropButton.enabled = CanCloseOnBackdropClick;
            }
        }
    }
}
