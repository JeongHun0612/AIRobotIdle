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

        [Header("Colors (Optional)")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.green;

        private EPopupUIType _currentTab = EPopupUIType.None;

        private void Awake()
        {
            var boundButtons = new HashSet<Button>();

            foreach (var item in _tabs)
            {
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

        private void BindTabClick(Button button, EPopupUIType tab, HashSet<Button> boundButtons)
        {
            if (button == null)
                return;

            if (boundButtons != null && !boundButtons.Add(button))
                return;

            button.onClick.AddListener(() => OnTabClicked(tab));
        }

        private void OnEnable()
        {
            // 현재 탭 상태 반영
            ApplySelection(_currentTab);
        }

        public void SetCurrent(EPopupUIType tab)
        {
            _currentTab = tab;
            ApplySelection(tab);
        }

        private void OnTabClicked(EPopupUIType tab)
        {
            Debug.Log($"[UIBottomBarMenu] Clicked: {tab}, Current: {_currentTab}");

            // 토글 로직: 이미 선택된 탭을 다시 누르면 닫기 (Battle로 돌아가기)
            if (_currentTab == tab && tab != EPopupUIType.None)
            {
                Debug.Log("[UIBottomBarMenu] Toggle detected. Closing popup.");
                OnTabClicked(EPopupUIType.None);
                return;
            }

            ApplySelection(tab);

            // UIManager 연동
            if (tab == EPopupUIType.None)
            {
                // Battle 탭: 모든 팝업 닫기 (메인 씬 보이기)
                UIManager.Instance.CloseAllPopups();
            }
            else
            {
                // 해당 팝업 열기
                UIManager.Instance.ShowPopup(tab);
            }
        }

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




