using UnityEngine;
using UnityEngine.UI;

namespace Reveal.UI
{
    /// <summary>
    /// Brief full-screen splash shown on launch (fox hero art), fading out
    /// after a short hold so the menu underneath is revealed. Skipped
    /// entirely if no splash art is imported.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        const float HoldSeconds = 1.1f;
        const float FadeSeconds = 0.5f;

        CanvasGroup _group;
        float _t;
        bool _fading;

        public static void ShowIfAvailable(Transform canvasParent)
        {
            var tex = GameArt.Splash;
            if (tex == null) return;

            var go = new GameObject("Splash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(canvasParent, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = GameArt.SpriteFrom(tex);
            img.preserveAspect = true;
            img.color = Color.white;
            UIFactory.Stretch(img.rectTransform);

            var bg = UIFactory.Panel(canvasParent, "SplashBg", UIFactory.Hex("#4a3aa8"));
            bg.SetSiblingIndex(go.transform.GetSiblingIndex());
            UIFactory.Stretch(bg);

            var s = go.AddComponent<SplashScreen>();
            s._group = go.GetComponent<CanvasGroup>();
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (!_fading && _t >= HoldSeconds) _fading = true;
            if (_fading)
            {
                float a = 1f - Mathf.Clamp01((_t - HoldSeconds) / FadeSeconds);
                _group.alpha = a;
                if (a <= 0f)
                {
                    // Destroy the paired background panel (previous sibling) too.
                    var bg = transform.parent.Find("SplashBg");
                    if (bg != null) Destroy(bg.gameObject);
                    Destroy(gameObject);
                }
            }
        }
    }
}
