using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    public class UI_Popup : UI_Base
    {
        public bool RememberInHistory = true;

        [Header("Input")]
        [SerializeField] private bool _isCloseOnBackdropClick = true;

        private bool _isBackdropCloseButtonProcessed = false;

        // 왜: 팝업마다 '빈 공간 클릭 닫힘' 정책이 다를 수 있어, 상속으로 정책을 재정의 가능하게 한다.
        protected virtual bool GetCloseOnBackdropClick()
        {
            return _isCloseOnBackdropClick;
        }

        // 왜: 팝업이 실제로 보이는 타이밍에만 Backdrop 닫기 정책을 적용해야, 프리팹 제작/초기화 순서 영향이 없다.
        public override void OnShow()
        {
            base.OnShow();
            TryDisableBackdropCloseIfNeeded();
        }

        // 왜: '빈 공간 클릭 닫힘'만 막고, Backdrop의 Raycast는 유지해
        //     팝업이 열려있는 동안 메인 화면(배틀/상시 UI 등) 입력이 새지 않게 한다.
        private void TryDisableBackdropCloseIfNeeded()
        {
            if (GetCloseOnBackdropClick())
                return;

            if (_isBackdropCloseButtonProcessed)
                return;

            var backdropButton = FindBackdropCloseButton();
            if (backdropButton == null)
                return;

            // 왜: 빈 공간 탭으로 닫히지 않게 하되, 팝업 바깥 입력(메인 화면 등)은 계속 막아야 한다.
            //     따라서 Backdrop 자체는 RaycastTarget을 유지하고, '닫기'만 발생하는 Button 컴포넌트만 비활성화한다.
            backdropButton.enabled = false;
            _isBackdropCloseButtonProcessed = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[UI_Popup] Backdrop close disabled: '{name}' (Button='{backdropButton.name}')");
#endif
        }

        // 왜: 프로젝트 프리팹마다 Backdrop 이름/구조가 다를 수 있어,
        //     '닫기 호출을 가진 버튼 중 가장 큰 영역'을 Backdrop 후보로 간주해 범용적으로 처리한다.
        private Button FindBackdropCloseButton()
        {
            var buttons = GetComponentsInChildren<Button>(includeInactive: true);
            if (buttons == null || buttons.Length == 0)
                return null;

            Button best = null;
            float bestArea = -1f;

            foreach (var button in buttons)
            {
                if (button == null)
                    continue;

                if (!HasPersistentCloseCall(button))
                    continue;

                float area = GetWorldArea(button.transform as RectTransform);
                if (area > bestArea)
                {
                    best = button;
                    bestArea = area;
                }
            }

            return best;
        }

        // 왜: 현재 프로젝트에서는 Backdrop이 UIManager.CloseCurrentPopup 또는 각 UI의 OnClickBack에 연결된 케이스가 있어,
        //     해당 호출이 붙은 버튼만 대상으로 삼아 오탐을 줄인다.
        private static bool HasPersistentCloseCall(Button button)
        {
            if (button == null)
                return false;

            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                string methodName = button.onClick.GetPersistentMethodName(i);
                if (string.IsNullOrEmpty(methodName))
                    continue;

                if (methodName == nameof(UIManager.CloseCurrentPopup))
                    return true;

                // 왜: 일부 프리팹은 Backdrop이 UI의 Back 버튼 메서드로 연결될 수 있다.
                //     이 경우도 '가장 큰 버튼(=대부분 Backdrop)'만 끄는 방식으로 안전하게 처리한다.
                if (methodName == "OnClickBack")
                    return true;
            }

            return false;
        }

        // 왜: Backdrop 후보를 '가장 큰 버튼'으로 판단하기 위해 월드 코너 기반 면적을 사용한다(해상도/스케일 영향 최소화).
        private static float GetWorldArea(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return 0f;

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            float width = Vector3.Distance(corners[0], corners[3]);
            float height = Vector3.Distance(corners[0], corners[1]);
            return width * height;
        }
    }
}
