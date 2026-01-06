using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SahurRaising
{
    public class UI_ShopPopup : UI_Popup
    {
        [Header("상점 타이틀 텍스트")]
        [SerializeField] private TMP_Text _titleText;

        [Header("상점 탭 버튼")]
        [SerializeField] private List<ShopTabButton> _tabButtons = new();

        [Header("상점 탭 UI 패널들")]
        [SerializeField] private List<ShopPanelBase> _shopPanels = new();

        private IGachaService _gachaService;

        private Dictionary<ShopType, ShopPanelBase> _panelDictionary;

        private ShopType _currentType = ShopType.Gacha;

        public async override UniTask InitializeAsync()
        {
            // 서비스 바인딩 시도 (실패 시 무시하고 진행)
            TryBindService();

            // 탭 버튼 이벤트 등록
            RegisterTabButtons();

            BuildPanelDictionary();

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            OnClickTabButton(_currentType);
        }

        private bool TryBindService()
        {
            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            return _gachaService != null;
        }

        private void RegisterTabButtons()
        {
            foreach (var tabButton in _tabButtons)
            {
                tabButton.Initialize();
                tabButton.Register(OnClickTabButton);
            }
        }

        private void BuildPanelDictionary()
        {
            _panelDictionary = new Dictionary<ShopType, ShopPanelBase>();

            foreach (var panel in _shopPanels)
            {
                if (panel != null)
                {
                    panel.Initialize();
                    panel.gameObject.SetActive(false);
                    _panelDictionary[panel.ShopType] = panel;
                }
            }
        }

        private void SwitchPanel(ShopType type)
        {
            // 모든 패널 비활성화
            foreach (var panel in _panelDictionary.Values)
            {
                if (panel != null)
                {
                    panel.Hide();
                }
            }

            // 선택된 패널 활성화
            if (_panelDictionary.TryGetValue(type, out var targetPanel))
            {
                targetPanel.Show();
            }
        }

        public void OnClickTabButton(ShopType type)
        {
            // 해당 타입의 탭 버튼 찾기
            ShopTabButton targetTabButton = null;
            foreach (var tabButton in _tabButtons)
            {
                if (tabButton.Type == type)
                {
                    targetTabButton = tabButton;
                    break;
                }
            }

            // 락 상태인 탭은 활성화하지 않음
            if (targetTabButton == null || targetTabButton.IsLock)
                return;

            foreach (var tabButton in _tabButtons)
            {
                tabButton.OnShow(tabButton.Type == type);
            }

            _titleText.text = targetTabButton.TitleText;
            _currentType = type;

            // 패널 전환
            SwitchPanel(type);
        }
    }
}
