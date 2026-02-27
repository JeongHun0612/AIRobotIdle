using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Services.LevelPlay;
using System.Collections.Generic;
using System;

namespace SahurRaising.Core
{
    public class AdvertisementService : IAdvertisementService
    {
        // LevelPlay App Key
        private const string ANDROID_APP_KEY = "255e5e405";
        private const string IOS_APP_KEY = "";

        private bool _isInitialized;
        private UniTaskCompletionSource<bool> _initTcs;

        // SO 매핑
        private AdUnitConfig _config;
        private readonly Dictionary<AdKey, AdUnitEntry> _adUnitMap = new();

        // Rewarded / Interstitial / Banner 인스턴스 캐시
        private readonly Dictionary<AdKey, LevelPlayRewardedAd> _rewardedAds = new();
        private readonly Dictionary<AdKey, LevelPlayInterstitialAd> _interstitialAds = new();
        private readonly Dictionary<AdKey, LevelPlayBannerAd> _bannerAds = new();

        private IResourceService _resourceService;

        public bool IsInitialized => _isInitialized;

        public AdvertisementService(IResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.Log("[AdvertisementService] 이미 초기화되었습니다.");
                return;
            }

            // AdUnitConfig 로드
            await LoadAdUnitConfig();
            if (_config == null)
            {
                Debug.LogError("[AdvertisementService] AdUnitConfig 로드 실패");
                return;
            }

            _initTcs = new UniTaskCompletionSource<bool>();

            // 이벤트 리스너 등록 (초기화 전에 등록해야 함)
            LevelPlay.ValidateIntegration();
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

            // 플랫폼별 App Key 선택
            string appKey = GetAppKey();
            Debug.Log($"[AdvertisementService] LevelPlay 초기화 시작... AppKey: {appKey}");

            // SDK 초기화
            LevelPlay.Init(appKey);

            bool initResult = await _initTcs.Task;

            // 이벤트 핸들러 해제
            LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;

            if (!initResult)
            {
                Debug.LogError("[AdvertisementService] LevelPlay 초기화 실패");
                return;
            }

            _isInitialized = true;
            Debug.Log("[AdvertisementService] LevelPlay 초기화 완료");
        }

        private async UniTask LoadAdUnitConfig()
        {
            _config = await _resourceService.LoadAssetAsync<AdUnitConfig>("AdUnitConfig");

            if (_config == null || _config.Entries == null)
                return;

            _adUnitMap.Clear();
            foreach (var entrie in _config.Entries)
            {
                if (!_adUnitMap.ContainsKey(entrie.Key))
                    _adUnitMap[entrie.Key] = entrie;
            }

            Debug.Log($"[AdvertisementService] AdUnitConfig 로드 완료. Count={_adUnitMap.Count}");
        }

        private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            Debug.Log("[AdvertisementService] LevelPlay 초기화 성공");
            _initTcs?.TrySetResult(true);
        }

        private void SdkInitializationFailedEvent(LevelPlayInitError error)
        {
            Debug.LogError($"[AdvertisementService] LevelPlay 초기화 실패: {error.ErrorMessage}");
            _initTcs?.TrySetResult(false);
        }

        private string GetAppKey()
        {
#if UNITY_ANDROID
            return ANDROID_APP_KEY;
#elif UNITY_IOS
            return IOS_APP_KEY;
#else
            return ANDROID_APP_KEY; // 에디터에서는 Android 키 사용
#endif
        }

        private string GetAdUnitId(AdKey key)
        {
            if (!_adUnitMap.TryGetValue(key, out var entry))
            {
                Debug.LogError($"[AdvertisementService] AdKey 매핑 없음: {key}");
                return null;
            }

#if UNITY_ANDROID
            return entry.AndroidAdUnitId;
#elif UNITY_IOS
            return entry.IosAdUnitId;
#else
            return entry.AndroidAdUnitId;
#endif
        }

        #region RewardedAds
        public async UniTask<bool> ShowRewardedAdAsync(AdKey key)
        {
            if (!_isInitialized)
                return false;

            var ad = GetOrCreateRewardedAd(key);
            if (ad == null)
                return false;

            if (!ad.IsAdReady())
            {
                var loadTcs = new UniTaskCompletionSource<bool>();

                void OnLoaded(LevelPlayAdInfo info)
                {
                    ad.OnAdLoaded -= OnLoaded;
                    ad.OnAdLoadFailed -= OnFailed;
                    loadTcs.TrySetResult(true);
                }

                void OnFailed(LevelPlayAdError error)
                {
                    ad.OnAdLoaded -= OnLoaded;
                    ad.OnAdLoadFailed -= OnFailed;
                    loadTcs.TrySetResult(false);
                }

                ad.OnAdLoaded += OnLoaded;
                ad.OnAdLoadFailed += OnFailed;

                Debug.Log($"[AdvertisementService] Rewarded LoadAd: {ad.AdUnitId}");
                ad.LoadAd();

                bool loadResult = await loadTcs.Task;
                if (!loadResult)
                    return false;
            }

            // 보상 여부 대기
            var rewardTcs = new UniTaskCompletionSource<bool>();

            void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
            {
                ad.OnAdRewarded -= OnRewarded;
                ad.OnAdDisplayFailed -= OnDisplayFailed;

                rewardTcs.TrySetResult(true);
            }

            void OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
            {
                ad.OnAdRewarded -= OnRewarded;
                ad.OnAdDisplayFailed -= OnDisplayFailed;

                rewardTcs.TrySetResult(false);
            }

            ad.OnAdRewarded += OnRewarded;
            ad.OnAdDisplayFailed += OnDisplayFailed;

            Debug.Log($"[AdvertisementService] Rewarded ShowAd: {ad.AdUnitId}");
            ad.ShowAd();

            try
            {
                return await rewardTcs.Task.Timeout(TimeSpan.FromSeconds(60));
            }
            finally
            {
                ad.OnAdRewarded -= OnRewarded;
                ad.OnAdDisplayFailed -= OnDisplayFailed;
            }
        }

        private LevelPlayRewardedAd GetOrCreateRewardedAd(AdKey key)
        {
            if (_rewardedAds.TryGetValue(key, out var ad))
                return ad;

            string adUnitId = GetAdUnitId(key);
            if (string.IsNullOrEmpty(adUnitId))
                return null;

            var rewardedAd = new LevelPlayRewardedAd(adUnitId);
            _rewardedAds[key] = rewardedAd;
            return rewardedAd;
        }
        #endregion

        #region InterstitialAds
        public async UniTask<bool> ShowInterstitialAdAsync(AdKey key)
        {
            if (!_isInitialized)
                return false;

            var ad = GetOrCreateInterstitialAd(key);
            if (ad == null)
                return false;

            if (!ad.IsAdReady())
            {
                var loadTcs = new UniTaskCompletionSource<bool>();

                void OnLoaded(LevelPlayAdInfo info)
                {
                    ad.OnAdLoaded -= OnLoaded;
                    ad.OnAdLoadFailed -= OnFailed;
                    loadTcs.TrySetResult(true);
                }

                void OnFailed(LevelPlayAdError error)
                {
                    ad.OnAdLoaded -= OnLoaded;
                    ad.OnAdLoadFailed -= OnFailed;
                    loadTcs.TrySetResult(false);
                }

                ad.OnAdLoaded += OnLoaded;
                ad.OnAdLoadFailed += OnFailed;

                Debug.Log($"[AdvertisementService] Interstitial LoadAd: {ad.AdUnitId}");
                ad.LoadAd();

                bool loadResult = await loadTcs.Task;
                if (!loadResult)
                    return false;
            }

            var closeTcs = new UniTaskCompletionSource<bool>();

            void OnClosed(LevelPlayAdInfo info)
            {
                ad.OnAdClosed -= OnClosed;
                ad.OnAdDisplayFailed -= OnDisplayFailed;
                closeTcs.TrySetResult(true);
            }

            void OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
            {
                ad.OnAdClosed -= OnClosed;
                ad.OnAdDisplayFailed -= OnDisplayFailed;
                closeTcs.TrySetResult(false);
            }

            ad.OnAdClosed += OnClosed;
            ad.OnAdDisplayFailed += OnDisplayFailed;

            Debug.Log($"[AdvertisementService] Interstitial ShowAd: {ad.AdUnitId}");
            ad.ShowAd();

            try
            {
                return await closeTcs.Task.Timeout(TimeSpan.FromSeconds(60));
            }
            finally
            {
                ad.OnAdClosed -= OnClosed;
                ad.OnAdDisplayFailed -= OnDisplayFailed;
            }
        }

        private LevelPlayInterstitialAd GetOrCreateInterstitialAd(AdKey key)
        {
            if (_interstitialAds.TryGetValue(key, out var ad))
                return ad;

            string adUnitId = GetAdUnitId(key);
            if (string.IsNullOrEmpty(adUnitId))
                return null;

            var interstitialAd = new LevelPlayInterstitialAd(adUnitId);
            _interstitialAds[key] = interstitialAd;

            return interstitialAd;
        }

        #endregion

        #region BannerAds
        public async UniTask<bool> ShowBannerAsync(
            AdKey key,
            BannerAdSize size = BannerAdSize.Banner,
            BannerAdPosition position = BannerAdPosition.BottomCenter,
            bool respectSafeArea = true,
            bool displayOnLoad = true,
            string placementName = null)
        {
            if (!_isInitialized)
                return false;

            var ad = GetOrCreateBannerAd(key, size, position, respectSafeArea, displayOnLoad, placementName);
            if (ad == null)
                return false;

            var loadTcs = new UniTaskCompletionSource<bool>();

            void OnLoaded(LevelPlayAdInfo info)
            {
                ad.OnAdLoaded -= OnLoaded;
                ad.OnAdLoadFailed -= OnFailed;
                loadTcs?.TrySetResult(true);
            }

            void OnFailed(LevelPlayAdError error)
            {
                ad.OnAdLoaded -= OnLoaded;
                ad.OnAdLoadFailed -= OnFailed;
                loadTcs?.TrySetResult(false);
            }

            ad.OnAdLoaded += OnLoaded;
            ad.OnAdLoadFailed += OnFailed;

            Debug.Log($"[AdvertisementService] Banner LoadAd: {ad.GetAdUnitId()}");
            ad.LoadAd();

            bool loaded = await loadTcs.Task;

            if (loaded && !displayOnLoad)
                ad.ShowAd();

            return loaded;
        }

        public void HideBanner(AdKey key)
        {
            if (!_isInitialized)
                return;

            if (!_bannerAds.TryGetValue(key, out var ad))
                return;

            ad.HideAd();
        }

        private LevelPlayBannerAd GetOrCreateBannerAd(AdKey key, BannerAdSize size, BannerAdPosition position, bool respectSafeArea, bool displayOnLoad, string placementName)
        {
            if (_bannerAds.TryGetValue(key, out var ad))
                return ad;

            string adUnitId = GetAdUnitId(key);
            if (string.IsNullOrEmpty(adUnitId))
                return null;

            var configBuilder = new LevelPlayBannerAd.Config.Builder();
            configBuilder.SetSize(ToLevelPlayAdSize(size));
            configBuilder.SetPosition(ToLevelPlayPosition(position));
            configBuilder.SetRespectSafeArea(respectSafeArea);
            configBuilder.SetDisplayOnLoad(displayOnLoad);
            if (!string.IsNullOrEmpty(placementName))
                configBuilder.SetPlacementName(placementName);

            var bannerConfig = configBuilder.Build();
            var bannerAd = new LevelPlayBannerAd(adUnitId, bannerConfig);
            _bannerAds[key] = bannerAd;
            return bannerAd;
        }

        private LevelPlayAdSize ToLevelPlayAdSize(BannerAdSize size)
        {
            return size switch
            {
                BannerAdSize.Large => LevelPlayAdSize.LARGE,
                BannerAdSize.MediumRectangle => LevelPlayAdSize.MEDIUM_RECTANGLE,
                BannerAdSize.Adaptive => LevelPlayAdSize.CreateAdaptiveAdSize(),
                _ => LevelPlayAdSize.BANNER,
            };
        }

        private LevelPlayBannerPosition ToLevelPlayPosition(BannerAdPosition pos)
        {
            return pos switch
            {
                BannerAdPosition.TopLeft => LevelPlayBannerPosition.TopLeft,
                BannerAdPosition.TopCenter => LevelPlayBannerPosition.TopCenter,
                BannerAdPosition.TopRight => LevelPlayBannerPosition.TopRight,
                BannerAdPosition.CenterLeft => LevelPlayBannerPosition.CenterLeft,
                BannerAdPosition.Center => LevelPlayBannerPosition.Center,
                BannerAdPosition.CenterRight => LevelPlayBannerPosition.CenterRight,
                BannerAdPosition.BottomLeft => LevelPlayBannerPosition.BottomLeft,
                BannerAdPosition.BottomCenter => LevelPlayBannerPosition.BottomCenter,
                BannerAdPosition.BottomRight => LevelPlayBannerPosition.BottomRight,
                _ => LevelPlayBannerPosition.BottomCenter,
            };
        }
        #endregion
    }
}
