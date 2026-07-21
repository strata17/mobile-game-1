using System;
using System.Collections;
using UnityEngine;

namespace Reveal.Ads
{
    /// <summary>
    /// Editor / no-SDK fallback. Shows a short simulated "SPONSORED" delay and
    /// always grants the reward, so the whole game loop is testable without an
    /// ad account. Used automatically when REVEAL_LEVELPLAY is not defined.
    /// </summary>
    public class MockAdService : MonoBehaviour, IAdService
    {
        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;

        public void Initialize() { }

        public void ShowRewarded(Action<bool> onDone)
        {
            StartCoroutine(Play(2.0f, () => onDone?.Invoke(true)));
        }

        public void ShowInterstitial(Action onDone)
        {
            StartCoroutine(Play(1.5f, () => onDone?.Invoke()));
        }

        IEnumerator Play(float seconds, Action done)
        {
            var overlay = AdOverlay.Show();
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                overlay.SetRemaining(Mathf.CeilToInt(seconds - t));
                yield return null;
            }
            overlay.Hide();
            done?.Invoke();
        }
    }
}
