using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SahurRaising
{
    public class UI_Gacha : UI_Popup
    {
        [Header("가챠 패널들")]
        [SerializeField] private List<GachaPanel> _gachaPanels;

        private IGachaService _gachaService;
        private IEventBus _eventBus;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            // 자식에서 GachaPanel들을 자동으로 찾기 (Inspector에서 할당하지 않은 경우)
            if (_gachaPanels == null || _gachaPanels.Count == 0)
            {
                _gachaPanels = GetComponentsInChildren<GachaPanel>().ToList();
            }

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            if (_eventBus != null)
            {
                _eventBus.Subscribe<GachaPullEvent>(OnGachaDraw);
                _eventBus.Subscribe<RewardGrantedEvent>(OnRewardGranted);
            }

            RefreshGachaPanel();
        }

        public override void OnHide()
        {
            base.OnHide();

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<GachaPullEvent>(OnGachaDraw);
                _eventBus.Unsubscribe<RewardGrantedEvent>(OnRewardGranted);
            }
        }

        private bool TryBindService()
        {
            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            if (_eventBus == null && ServiceLocator.HasService<IEventBus>())
            {
                _eventBus = ServiceLocator.Get<IEventBus>();
            }

            return _gachaService != null && _eventBus != null;
        }

        private void RefreshGachaPanel()
        {
            if (_gachaPanels == null || _gachaPanels.Count == 0)
                return;

            // 각 가챠 패널 업데이트
            foreach (var panel in _gachaPanels)
            {
                panel?.Refresh();
            }
        }

        private void OnRewardGranted(RewardGrantedEvent evt)
        {
            if (evt.CurrencyType == CurrencyType.Diamond)
            {
                RefreshGachaPanel();
            }
        }

        /// <summary>
        /// 가챠 뽑기 이벤트 처리
        /// </summary>
        private void OnGachaDraw(GachaPullEvent evt)
        {
            // UI_GachaResult 팝업 열기
            var gachaResultPopup = UIManager.Instance.ShowPopup<UI_GachaResult>(EPopupUIType.GachaResult);
            if (gachaResultPopup != null)
            {
                gachaResultPopup.SetGachaResult(evt);
            }
            else
            {
                Debug.LogWarning("[UI_Gacha] UI_GachaResult 팝업을 찾을 수 없습니다.");
            }

            RefreshGachaPanel();
        }
    }
}
