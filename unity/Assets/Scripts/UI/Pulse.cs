using UnityEngine;

namespace Reveal.UI
{
    /// <summary>Gentle continuous scale pulse to draw the eye (e.g. tutorial hint).</summary>
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.05f;
        public float speed = 3.2f;
        float _t;

        void Update()
        {
            _t += Time.unscaledDeltaTime * speed;
            float s = 1f + Mathf.Sin(_t) * amount;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
