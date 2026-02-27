using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using SahurRaising.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_OfflineResult : UI_Popup
    {
        [Header("UI 요소")]
        [SerializeField] private TMP_Text _offlineTimeText;
        [SerializeField] private TMP_Text _rewardAmountText;

        [Header("버튼")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _claimDoubleButton; // 광고 2배 수령 버튼

        private OfflineRewardInfo? _offlineRewardInfo;

        private ICurrencyService _currencyService;
        private IAdvertisementService _advertisementService;

        public override async UniTask InitializeAsync()
        {
            TryBindService();

            // 버튼 이벤트 등록
            RegisterButtonEvents();

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            UpdateOfflineRewardInfo();
        }

        private void RegisterButtonEvents()
        {
            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveAllListeners();
                _claimButton.onClick.AddListener(OnClickClaim);
            }

            if (_claimDoubleButton != null)
            {
                _claimDoubleButton.onClick.RemoveAllListeners();
                _claimDoubleButton.onClick.AddListener(OnClickClaimDouble);
            }
        }

        private bool TryBindService()
        {
            if (_currencyService == null && ServiceLocator.HasService<ICurrencyService>())
            {
                _currencyService = ServiceLocator.Get<ICurrencyService>();
            }

            if (_advertisementService == null && ServiceLocator.HasService<IAdvertisementService>())
            {
                _advertisementService = ServiceLocator.Get<IAdvertisementService>();
            }

            return _currencyService != null && _advertisementService != null;
        }

        private void UpdateOfflineRewardInfo()
        {
            // CurrencyService에서 오프라인 보상 정보 가져오기
            _offlineRewardInfo = _currencyService.GetOfflineRewardInfo();

            if (_offlineRewardInfo.HasValue)
            {
                var info = _offlineRewardInfo.Value;

                // 오프라인 시간 포맷팅 (00시00분 형식)
                var clampedMinutes = (int)(info.ClampedSeconds / 60);
                var hours = clampedMinutes / 60;
                var minutes = clampedMinutes % 60;

                var maxHours = (int)(info.MaxSeconds / 3600);
                var maxMinutes = (int)((info.MaxSeconds % 3600) / 60);

                _offlineTimeText.text = $"{hours:D2}시{minutes:D2}분 (최대 {maxHours}시간{maxMinutes:D2}분)";
                _rewardAmountText.text = NumberFormatUtil.FormatBigDouble(info.RewardAmount);
            }
        }

        public void OnClickClaim()
        {
            if (!_offlineRewardInfo.HasValue || _currencyService == null)
                return;

            var rewardAmount = _offlineRewardInfo.Value.RewardAmount;

            // CurrencyService의 Add 함수를 호출하여 보상 지급
            _currencyService.Add(CurrencyType.Gold, rewardAmount, "OfflineReward");

            Debug.Log($"[UI_OfflineResult] 오프라인 보상 수령: {rewardAmount}");

            // 팝업 닫기
            UIManager.Instance.CloseCurrentPopup();
        }

        public async void OnClickClaimDouble()
        {
            if (!_offlineRewardInfo.HasValue || _currencyService == null || _advertisementService == null)
                return;

            var rewardAmount = _offlineRewardInfo.Value.RewardAmount;

            try
            {
                bool completed = await _advertisementService.ShowRewardedAdAsync(AdKey.Rewarded_Default);
                if (completed)
                {
                    rewardAmount *= 2;
                }
                Debug.Log($"[UI_OfflineResult] 오프라인 보상 2배 수령: {rewardAmount}");
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[UI_OfflineResult] 광고 응답 시간 초과. 기본 보상만 지급합니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UI_OfflineResult] 광고 시청 실패: {ex.Message}");
            }

            _currencyService.Add(CurrencyType.Gold, rewardAmount, "OfflineReward_Double");

            // 팝업 닫기
            UIManager.Instance.CloseCurrentPopup();
        }
    }
}
