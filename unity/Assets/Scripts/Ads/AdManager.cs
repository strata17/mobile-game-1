using System;
using UnityEngine;

namespace Reveal.Ads
{
    /// <summary>
    /// Game-facing entry point for ads. Owns the active IAdService and applies
    /// the monetization pacing policy (no interstitial pressure in the first
    /// session, and only every Nth loss thereafter).
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        IAdService _service;
        int _lossCount;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Pick the implementation. LevelPlayAdService compiles to a mock
            // passthrough until the SDK + REVEAL_LEVELPLAY symbol are present.
#if REVEAL_LEVELPLAY
            _service = gameObject.AddComponent<LevelPlayAdService>();
#else
            _service = gameObject.AddComponent<MockAdService>();
#endif
            _service.Initialize();
        }

        public void ShowRewarded(Action<bool> onDone) => _service.ShowRewarded(onDone);

        /// <summary>
        /// Interstitial gated by policy. Returns true if an ad was actually
        /// shown (caller can still proceed on onDone either way).
        /// </summary>
        public bool MaybeShowInterstitialOnLoss(int currentLevel, Action onDone)
        {
            _lossCount++;
            if (currentLevel < Core.GameConfig.NoAdsBeforeLevel ||
                _lossCount % Core.GameConfig.InterstitialEveryNLosses != 0)
            {
                onDone?.Invoke();
                return false;
            }
            _service.ShowInterstitial(onDone);
            return true;
        }
    }
}
