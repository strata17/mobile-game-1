using System;
using UnityEngine;

namespace Reveal.Ads
{
    /// <summary>
    /// Real ad mediation via ironSource LevelPlay (AppLovin MAX). This is the
    /// production path for an ad-monetized casual game.
    ///
    /// SETUP (see unity/README.md for the full checklist):
    ///   1. Install the LevelPlay Unity SDK (Package Manager or .unitypackage).
    ///   2. Player Settings -> Scripting Define Symbols: add REVEAL_LEVELPLAY.
    ///   3. Fill in AppKey below (and per-platform ad unit ids if you use them).
    ///
    /// Everything is wrapped in #if REVEAL_LEVELPLAY so the project still
    /// compiles and runs (against MockAdService) before the SDK is imported.
    /// </summary>
    public class LevelPlayAdService : MonoBehaviour, IAdService
    {
#if REVEAL_LEVELPLAY
        [SerializeField] string androidAppKey = "YOUR_ANDROID_APP_KEY";
        [SerializeField] string iosAppKey = "YOUR_IOS_APP_KEY";

        Action<bool> _rewardCb;
        Action _interstitialCb;
        bool _rewardEarned;

        public bool IsRewardedReady => IronSource.Agent.isRewardedVideoAvailable();
        public bool IsInterstitialReady => IronSource.Agent.isInterstitialReady();

        public void Initialize()
        {
            string key = Application.platform == RuntimePlatform.IPhonePlayer ? iosAppKey : androidAppKey;

            IronSourceEvents.onSdkInitializationCompletedEvent += () =>
                IronSource.Agent.loadInterstitial();

            // Rewarded video
            IronSourceRewardedVideoEvents.onAdRewardedEvent += (info, placement) => _rewardEarned = true;
            IronSourceRewardedVideoEvents.onAdClosedEvent += info =>
            {
                var cb = _rewardCb; _rewardCb = null;
                cb?.Invoke(_rewardEarned);
                _rewardEarned = false;
            };

            // Interstitial
            IronSourceInterstitialEvents.onAdClosedEvent += info =>
            {
                var cb = _interstitialCb; _interstitialCb = null;
                IronSource.Agent.loadInterstitial(); // preload the next one
                cb?.Invoke();
            };
            IronSourceInterstitialEvents.onAdShowFailedEvent += (err, info) =>
            {
                var cb = _interstitialCb; _interstitialCb = null;
                IronSource.Agent.loadInterstitial();
                cb?.Invoke();
            };

            IronSource.Agent.init(key);
        }

        void OnApplicationPause(bool paused) => IronSource.Agent.onApplicationPause(paused);

        public void ShowRewarded(Action<bool> onDone)
        {
            if (!IsRewardedReady) { onDone?.Invoke(false); return; }
            _rewardCb = onDone;
            _rewardEarned = false;
            IronSource.Agent.showRewardedVideo();
        }

        public void ShowInterstitial(Action onDone)
        {
            if (!IsInterstitialReady) { onDone?.Invoke(); return; }
            _interstitialCb = onDone;
            IronSource.Agent.showInterstitial();
        }
#else
        // SDK not imported yet — behave like the mock so the game is playable.
        MockAdService _fallback;
        MockAdService Fallback => _fallback != null ? _fallback : (_fallback = gameObject.AddComponent<MockAdService>());

        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;
        public void Initialize() { Debug.LogWarning("[Reveal] LevelPlay SDK not imported; using mock ads. Define REVEAL_LEVELPLAY after importing the SDK."); }
        public void ShowRewarded(Action<bool> onDone) => Fallback.ShowRewarded(onDone);
        public void ShowInterstitial(Action onDone) => Fallback.ShowInterstitial(onDone);
#endif
    }
}
