using Reveal.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Reveal.Ads
{
    /// <summary>Full-screen "SPONSORED" overlay used by the mock ad service.</summary>
    public class AdOverlay : MonoBehaviour
    {
        Text _timer;

        public static AdOverlay Show()
        {
            var canvasGo = new GameObject("AdOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var overlay = canvasGo.AddComponent<AdOverlay>();

            var bg = UIFactory.Panel(canvasGo.transform, "BG", UIFactory.Hex("#0b0c10"));
            UIFactory.Stretch(bg);

            UIFactory.Label(bg, "Sponsored", "SPONSORED", 30, new Color(1, 1, 1, 0.5f))
                .rectTransform.anchoredPosition = new Vector2(0, 320);

            overlay._timer = UIFactory.Label(bg, "Timer", "Loading ad…", 44, Color.white);
            return overlay;
        }

        public void SetRemaining(int seconds)
        {
            if (_timer != null) _timer.text = seconds > 0 ? $"Ad · {seconds}s" : "Thanks!";
        }

        public void Hide()
        {
            if (this != null) Destroy(gameObject);
        }
    }
}
