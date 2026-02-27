using Cysharp.Threading.Tasks;

namespace SahurRaising.Core
{
    // 배너 사이즈 추상화
    public enum BannerAdSize
    {
        Banner,
        Large,
        MediumRectangle,
        Adaptive,
    }

    // 배너 위치 추상화
    public enum BannerAdPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public interface IAdvertisementService
    {
        bool IsInitialized { get; }

        UniTask InitializeAsync();

        UniTask<bool> ShowRewardedAdAsync(AdKey key);
        UniTask<bool> ShowInterstitialAdAsync(AdKey key);
        UniTask<bool> ShowBannerAsync(
            AdKey key,
            BannerAdSize size = BannerAdSize.Banner,
            BannerAdPosition position = BannerAdPosition.BottomCenter,
            bool respectSafeArea = true,
            bool displayOnLoad = true,
            string placementName = null);

        void HideBanner(AdKey key);
    }
}