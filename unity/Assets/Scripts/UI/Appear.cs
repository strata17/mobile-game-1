using UnityEngine;

namespace Reveal.UI
{
    /// <summary>
    /// Spawn "pop": scales the element from 0 up past 1 with a small
    /// overshoot, then settles. Used for clue discs and bomb marks so new
    /// information lands with a bounce instead of blinking into existence.
    /// </summary>
    public class Appear : MonoBehaviour
    {
        const float Dur = 0.22f;
        float _t;

        void OnEnable() { transform.localScale = Vector3.zero; }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / Dur);
            // ease-out-back: overshoot to ~1.1 then settle at 1
            float c1 = 1.70158f, c3 = c1 + 1f;
            float s = 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            transform.localScale = new Vector3(s, s, 1f);
            if (k >= 1f) { transform.localScale = Vector3.one; Destroy(this); }
        }
    }
}
