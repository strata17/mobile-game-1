using UnityEngine;

namespace Reveal.UI
{
    /// <summary>
    /// Quick pop-away used when a cover tile is revealed: a brief scale-up then
    /// shrink-to-nothing, then self-destruct. Adds tactile "juice" to the
    /// scratch without needing any animation assets.
    /// </summary>
    public class Vanish : MonoBehaviour
    {
        float _t;
        const float Dur = 0.16f;

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = _t / Dur;
            if (k >= 1f) { Destroy(gameObject); return; }
            // overshoot to 1.12 then down to 0
            float s = k < 0.35f ? Mathf.Lerp(1f, 1.12f, k / 0.35f)
                                : Mathf.Lerp(1.12f, 0f, (k - 0.35f) / 0.65f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
