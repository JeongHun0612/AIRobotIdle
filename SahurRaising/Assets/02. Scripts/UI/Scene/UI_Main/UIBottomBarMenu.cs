using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SahurRaising.UI
{
    /// <summary>
    /// 하단 바 탭을 중앙에서 관리한다.
    /// - BottomBar_Menu 같은 부모 오브젝트에 부착
    /// - 버튼/일반/포커스 오브젝트를 한 곳에서 토글
    /// </summary>
    public class UIBottomBarMenu : MonoBehaviour
    {
        /// <summary>
        /// 왜: 포커스/노멀 비주얼이 Button 오브젝트의 자식이 아닐 수 있어,
        ///     비주얼을 클릭해도 Button.onClick이 호출되지 않는 케이스가 발생한다.
        ///     (프리팹 구조를 강제하지 않기 위해) 필요할 때만 클릭을 탭 로직으로 전달한다.
        /// </summary>
        private sealed class TabClickForwarder : MonoBehaviour, IPointerClickHandler
        {
            private UIBottomBarMenu _owner;
            private EPopupUIType _tab;

            public void Initialize(UIBottomBarMenu owner, EPopupUIType tab)
            {
                _owner = owner;
                _tab = tab;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (_owner == null)
                    return;

                _owner.OnTabClicked(_tab);
            }
        }

        [System.Serializable]
        private class TabItem
        {
            public EPopupUIType PopupType; // None = Battle
            // 왜: Normal/Focus 비주얼은 토글하되, 터치(클릭) 대상은 항상 1개 버튼으로 고정한다.
            //     (Normal/Focus 각각 다른 Button을 쓰면, 선택 상태에 따라 클릭이 끊기는 문제가 발생한다.)
            public Button Button;
            public GameObject NormalRoot;
            public GameObject FocusRoot;
            public Image Icon; // 색상 토글이 필요할 때만 할당
        }

        [Header("Tabs")]
        [SerializeField] private List<TabItem> _tabs = new();

        [Header("Layer (Keep clickable over popups)")]
        [SerializeField] private bool _overrideSorting = true;
        [SerializeField] private int _sortingOrder = 10000;

        [Header("Colors (Optional)")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.green;

        private EPopupUIType _currentTab = EPopupUIType.None;
        private readonly HashSet<EPopupUIType> _configuredTabTypes = new HashSet<EPopupUIType>();

        // 왜: 탭 버튼 구조는 프리팹마다 다를 수 있어, Awake에서 클릭 바인딩/포워더를 한 번만 구성한다.
        private void Awake()
        {
            EnsureTopLayerIfNeeded();

            var boundButtons = new HashSet<Button>();

            foreach (var item in _tabs)
            {
                if (item != null)
                    _configuredTabTypes.Add(item.PopupType);

                BindTabClick(item.Button, item.PopupType, boundButtons);

                // 비주얼이 Button의 자식이 아닐 때만 포워더를 붙여 클릭이 끊기지 않게 한다.
                AttachForwarderIfNeeded(item.NormalRoot, item.Button, item.PopupType);
                AttachForwarderIfNeeded(item.FocusRoot, item.Button, item.PopupType);

                if (item.Button == null)
                {
                    Debug.LogWarning($"[UIBottomBarMenu] Tab '{item.PopupType}'에 연결된 Button이 없습니다. (권장) 탭 루트에 Button 1개만 할당하세요.");
                }
            }
        }

        // 왜: 하단바는 팝업 열림/닫힘에 따라 선택 상태가 바뀌어야 하므로, UIManager 이벤트를 생명주기 동안만 구독한다.
        private void OnEnable()
        {
            // 현재 탭 상태 반영
            ApplySelection(_currentTab);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PopupShown += HandlePopupShown;
                UIManager.Instance.PopupHidden += HandlePopupHidden;
            }
        }

        // 왜: 비활성화된 오브젝트가 이벤트를 계속 받으면 의도치 않은 선택 변경이 발생할 수 있어 구독을 해제한다.
        private void OnDisable()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PopupShown -= HandlePopupShown;
                UIManager.Instance.PopupHidden -= HandlePopupHidden;
            }
        }

        // 왜: 팝업의 Dim이 화면을 덮더라도 하단바는 항상 클릭 가능해야 하므로, 상단 정렬(Canvas overrideSorting)로 고정한다.
        private void EnsureTopLayerIfNeeded()
        {
            if (!_overrideSorting)
                return;

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = _sortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        // 왜: Normal/Focus 비주얼이 Button의 자식이 아닐 때 클릭이 끊기는 문제가 있어,
        //     필요한 경우에만 포워더를 붙여 '탭 클릭'을 강제로 전달한다.
        private void AttachForwarderIfNeeded(GameObject root, Button button, EPopupUIType tab)
        {
            if (root == null)
                return;

            // Button이 있고, root가 그 자식이라면 클릭은 자연스럽게 상위(Button)로 전달되므로 포워더 불필요.
            if (button != null && root.transform.IsChildOf(button.transform))
                return;

            var forwarder = root.GetComponent<TabClickForwarder>();
            if (forwarder == null)
                forwarder = root.AddComponent<TabClickForwarder>();

            forwarder.Initialize(this, tab);
        }

        // 왜: 버튼이 중복 바인딩되면 클릭이 여러 번 발생하므로, HashSet으로 한 번만 리스너를 등록한다.
        private void BindTabClick(Button button, EPopupUIType tab, HashSet<Button> boundButtons)
        {
            if (button == null)
                return;

            if (boundButtons != null && !boundButtons.Add(button))
                return;

            button.onClick.AddListener(() => OnTabClicked(tab));
        }

        // 왜: 외부에서 탭 상태를 강제로 동기화해야 할 때(팝업 닫힘/열림 이벤트 등) 사용한다.
        public void SetCurrent(EPopupUIType tab)
        {
            _currentTab = tab;
            ApplySelection(tab);
        }

        // 왜: UX 규칙(같은 탭 재클릭=닫기, 다른 탭=현재 닫고 대상 열기)을 하단바에서 단일 책임으로 처리한다.
        private void OnTabClicked(EPopupUIType tab)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[UIBottomBarMenu] Clicked: {tab}, Current: {_currentTab}");
#endif

            // 토글 로직: 이미 선택된 탭을 다시 누르면 닫기 (Battle로 돌아가기)
            if (_currentTab == tab && tab != EPopupUIType.None)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[UIBottomBarMenu] Toggle detected. Closing popup.");
#endif
                OnTabClicked(EPopupUIType.None);
                return;
            }

            ApplySelection(tab);

            // UIManager 연동
            if (UIManager.Instance == null)
                return;

            if (tab == EPopupUIType.None)
            {
                // Battle 탭: 모든 팝업 닫기 (메인 씬 보이기)
                UIManager.Instance.CloseAllPopups();
            }
            else
            {
                // 왜: 탭 전환은 '단일 팝업' UX여야 한다. (다른 탭 선택 시 기존 팝업은 닫히고 대상만 열린다.)
                UIManager.Instance.CloseAllPopups();

                // 해당 팝업 열기
                UIManager.Instance.ShowPopup(tab);
            }
        }

        // 왜: 팝업이 다른 경로(Back 버튼 등)로 열려도, 하단바 상태가 뒤처지지 않게 이벤트로 동기화한다.
        private void HandlePopupShown(EPopupUIType popupType)
        {
            if (!_configuredTabTypes.Contains(popupType))
                return;

            if (popupType == EPopupUIType.None)
                return;

            SetCurrent(popupType);
        }

        // 왜: 현재 선택된 탭 팝업이 닫히면, 하단바는 Battle(None) 상태로 돌아가야 한다.
        private void HandlePopupHidden(EPopupUIType popupType)
        {
            if (!_configuredTabTypes.Contains(popupType))
                return;

            if (_currentTab != popupType)
                return;

            SetCurrent(EPopupUIType.None);
        }

        // 왜: 선택 상태에 따른 비주얼 토글은 UI 계층/프리팹 차이를 숨기고 일관된 표현을 제공한다.
        private void ApplySelection(EPopupUIType tab)
        {
            _currentTab = tab;

            foreach (var item in _tabs)
            {
                bool isSelected = item.PopupType == tab;

                if (item.NormalRoot != null)
                    item.NormalRoot.SetActive(!isSelected);
                if (item.FocusRoot != null)
                    item.FocusRoot.SetActive(isSelected);
                if (item.Icon != null)
                    item.Icon.color = isSelected ? _selectedColor : _normalColor;
            }
        }
    }
}




