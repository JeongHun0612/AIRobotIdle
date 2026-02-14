using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_AdvanceResult : UI_Popup
    {
        [Header("결과슬롯 리스트")]
        [SerializeField] private List<AdvanceResultItemSlot> _itemSlots = new List<AdvanceResultItemSlot>();

        private IConfigService _configService;
        private IGachaService _gachaService;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;
        }

        private bool TryBindService()
        {
            if (_configService == null && ServiceLocator.HasService<IConfigService>())
            {
                _configService = ServiceLocator.Get<IConfigService>();
            }

            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            return _configService != null && _gachaService != null;
        }

        public void SetAdvanceResult(AdvanceResult? result)
        {
            if (result == null)
            {
                Debug.LogWarning("[UI_AdvanceResult] 강화 결과가 null입니다.");

                // 모든 슬롯 비활성화
                SetAdvanceResult(new List<AdvanceResult>());
                return;
            }

            // 단일 결과를 리스트로 변환하여 처리
            SetAdvanceResult(new List<AdvanceResult> { result.Value });
        }

        public void SetAdvanceResult(List<AdvanceResult> results)
        {
            if (results == null)
            {
                Debug.LogWarning("[UI_AdvanceResult] 강화 결과 리스트가 null입니다.");
                return;
            }

            if (!TryBindService())
                return;

            int resultCount = results.Count;
            int slotCount = _itemSlots.Count;

            // 리스트 카운트에 따라 ItemSlot 활성화/비활성화 및 데이터 전달
            for (int i = 0; i < slotCount; i++)
            {
                if (i < resultCount)
                {
                    var result = results[i];

                    // 각 결과의 타입에 맞는 전략 가져오기
                    var strategy = _gachaService.GetResultStrategy(result.Type);

                    // 슬롯 활성화 및 데이터 전달
                    _itemSlots[i].gameObject.SetActive(true);
                    _itemSlots[i].UpdateUI(result, _configService, strategy);
                }
                else
                {
                    // 사용하지 않는 슬롯 비활성화
                    _itemSlots[i].gameObject.SetActive(false);
                }
            }

            // 결과가 슬롯보다 많은 경우 경고
            if (resultCount > slotCount)
            {
                Debug.LogWarning($"[UI_AdvanceResult] 결과 개수({resultCount})가 슬롯 개수({slotCount})보다 많습니다.");
            }
        }
    }
}
