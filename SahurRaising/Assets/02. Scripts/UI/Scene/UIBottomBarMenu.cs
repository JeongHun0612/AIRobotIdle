using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    /// <summary>
    /// 하단 바 탭을 중앙에서 관리한다.
    /// - BottomBar_Menu 같은 부모 오브젝트에 부착
    /// - 버튼/일반/포커스 오브젝트를 한 곳에서 토글
    /// </summary>
    public class UIBottomBarMenu : MonoBehaviour
    {
        [System.Serializable]
        private class TabItem
        {
            public EUITabType Tab;
            public Button Button;
            public GameObject NormalRoot;
            public GameObject FocusRoot;
            public Image Icon; // 색상 토글이 필요할 때만 할당
        }

        [Header("Refs")]
        [SerializeField] private UIMainRootScene _rootScene;

        [Header("Tabs")]
        [SerializeField] private List<TabItem> _tabs = new();

        [Header("Colors (Optional)")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.green;

        private EUITabType _currentTab = EUITabType.Battle;

        private void Awake()
        {
            foreach (var item in _tabs)
            {
                if (item.Button == null)
                    continue;

                var captured = item;
                item.Button.onClick.AddListener(() => OnTabClicked(captured.Tab));
            }
        }

        private void OnEnable()
        {
            // 현재 탭 상태 반영
            ApplySelection(_currentTab, invokeRoot: false);
        }

        public void SetCurrent(EUITabType tab)
        {
            _currentTab = tab;
            ApplySelection(tab, invokeRoot: false);
        }

        private void OnTabClicked(EUITabType tab)
        {
            ApplySelection(tab, invokeRoot: true);
        }

        private void ApplySelection(EUITabType tab, bool invokeRoot)
        {
            _currentTab = tab;

            foreach (var item in _tabs)
            {
                bool isSelected = item.Tab == tab;

                if (item.NormalRoot != null)
                    item.NormalRoot.SetActive(!isSelected);
                if (item.FocusRoot != null)
                    item.FocusRoot.SetActive(isSelected);
                if (item.Icon != null)
                    item.Icon.color = isSelected ? _selectedColor : _normalColor;
            }

            if (invokeRoot && _rootScene != null)
            {
                _rootScene.OpenTab(tab);
            }
        }
    }
}

