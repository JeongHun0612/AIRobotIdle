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
            // 디버그 로그 출력
            Debug.Log($"[UI_Gacha] ========== 가챠 결과 ({evt.Type}, {evt.Results.Count}개) ==========");

            var equipmentService = ServiceLocator.Get<IEquipmentService>();

            for (int i = 0; i < evt.Results.Count; i++)
            {
                var result = evt.Results[i];
                string itemInfo = "";

                if (result.Type == GachaType.Equipment)
                {
                    // 장비인 경우 등급 정보 조회
                    if (equipmentService != null && equipmentService.TryGetByCode(result.ItemCode, out var equipment))
                    {
                        itemInfo = $"Code: {result.ItemCode}, Grade: {equipment.Grade}, Type: {equipment.Type}";
                    }
                    else
                    {
                        itemInfo = $"Code: {result.ItemCode} (등급 정보 조회 실패)";
                    }
                }
                else if (result.Type == GachaType.Drone)
                {
                    // 드론인 경우 ID만 표시
                    itemInfo = $"ID: {result.ItemCode}";
                }
                else
                {
                    // 기타 타입
                    itemInfo = $"ItemCode: {result.ItemCode}";
                }

                Debug.Log($"[UI_Gacha] [{i + 1}/{evt.Results.Count}] {result.Type} - {itemInfo}");
            }

            Debug.Log("[UI_Gacha] ==========================================");


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
