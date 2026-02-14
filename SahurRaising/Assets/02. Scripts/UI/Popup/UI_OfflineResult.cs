using BreakInfinity;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using SahurRaising.Utils;
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

        private ICurrencyService _currencyService;
        private OfflineRewardInfo? _offlineRewardInfo;

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

            return _currencyService != null;
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

        public void OnClickClaimDouble()
        {
            if (!_offlineRewardInfo.HasValue || _currencyService == null)
                return;

            // TODO: 광고 기능 구현

            // 광고 시청 후 2배 보상 지급
            var rewardAmount = _offlineRewardInfo.Value.RewardAmount * 2;
            _currencyService.Add(CurrencyType.Gold, rewardAmount, "OfflineReward_Double");

            Debug.Log($"[UI_OfflineResult] 오프라인 보상 2배 수령: {rewardAmount}");

            // 팝업 닫기
            UIManager.Instance.CloseCurrentPopup();
        }
    }
}
