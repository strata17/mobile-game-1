using UnityEngine;

namespace Reveal.UI
{
    /// <summary>
    /// Insets a full-screen RectTransform to the device safe area so HUD chrome
    /// never sits under a notch, Dynamic Island or the home indicator. Re-applies
    /// on orientation/resolution changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rt;
        Rect _last;
        Vector2Int _lastRes;

        void Awake() { _rt = GetComponent<RectTransform>(); }
        void OnEnable() { Apply(); }

        void Update()
        {
            if (Screen.safeArea != _last || Screen.width != _lastRes.x || Screen.height != _lastRes.y)
                Apply();
        }

        void Apply()
        {
            _last = Screen.safeArea;
            _lastRes = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width == 0 || Screen.height == 0) return;

            Vector2 min = _last.position;
            Vector2 max = _last.position + _last.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
