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
        [Header("상점 탭 버튼")]
        [SerializeField] private List<ShopTabButton> _tabButtons = new();

        private IGachaService _gachaService;

        private EPopupUIType _currentType = EPopupUIType.None;

        public async override UniTask InitializeAsync()
        {
            // 서비스 바인딩 시도 (실패 시 무시하고 진행)
            TryBindService();

            // 탭 버튼 이벤트 등록
            RegisterTabButtons();

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            if (_currentType == EPopupUIType.None)
            {
                OnClickTabButton(EPopupUIType.Gacha);
            }
            else
            {
                OnClickTabButton(_currentType);
            }
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

        public void OnClickTabButton(EPopupUIType type)
        {
            if (UIManager.Instance == null)
                return;

            // 현재 열려있는 팝업과 같은 타입의 탭이면 반환
            var currentPopupType = UIManager.Instance.GetCurrentPopupType();
            if (currentPopupType == type || type == EPopupUIType.None)
                return;

            foreach (var tabButton in _tabButtons)
            {
                tabButton.OnShow(tabButton.Type == type);
            }

            UIManager.Instance.ClosePopups(1);
            UIManager.Instance.ShowPopup(type);

            _currentType = type;
        }
    }
}
