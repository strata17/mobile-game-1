using System;

namespace Reveal.Ads
{
    /// <summary>
    /// Abstraction over the ad network so gameplay never touches an SDK
    /// directly. Swap MockAdService for LevelPlayAdService (AppLovin MAX) by
    /// defining the REVEAL_LEVELPLAY scripting symbol — no gameplay changes.
    /// </summary>
    public interface IAdService
    {
        void Initialize();

        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }

        /// <summary>Show a rewarded video. onDone(true) if the reward was earned.</summary>
        void ShowRewarded(Action<bool> onDone);

        /// <summary>Show an interstitial. onDone fires when it is dismissed.</summary>
        void ShowInterstitial(Action onDone);
    }
}
