using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SahurRaising
{
    public class UI_Gacha : ShopPanelBase
    {
        [Header("가챠 패널들")]
        [SerializeField] private List<GachaPanel> _gachaPanels;

        private IGachaService _gachaService;
        private IEventBus _eventBus;

        public override void Initialize()
        {
            base.Initialize();

            // 자식에서 GachaPanel들을 자동으로 찾기 (Inspector에서 할당하지 않은 경우)
            if (_gachaPanels == null || _gachaPanels.Count == 0)
            {
                _gachaPanels = GetComponentsInChildren<GachaPanel>().ToList();
            }

            // 각 GachaPanel 초기화
            foreach (var panel in _gachaPanels)
            {
                panel?.Initialize();
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            if (_eventBus == null)
                _eventBus = ServiceLocator.Get<IEventBus>();

            if (_gachaService == null)
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_eventBus == null || _gachaService == null)
                return;

            if (_eventBus != null)
            {
                _eventBus.Subscribe<GachaPullEvent>(OnGachaDraw);
            }

            // 각 가챠 패널 업데이트
            if (_gachaPanels != null)
            {
                foreach (var panel in _gachaPanels)
                {
                    panel?.Refresh();
                }
            }
        }

        public override void OnHide()
        {
            base.OnHide();

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<GachaPullEvent>(OnGachaDraw);
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

            // 해당 타입의 GachaPanel 찾기
            var panel = _gachaPanels?.FirstOrDefault(p => p.GachaType == evt.Type);
            if (panel != null)
            {
                panel.Refresh();
            }
            else
            {
                Debug.LogWarning($"[UI_Gacha] {evt.Type} 타입의 GachaPanel을 찾을 수 없습니다.");
            }
        }
    }
}
