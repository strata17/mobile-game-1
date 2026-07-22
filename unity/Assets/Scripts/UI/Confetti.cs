using UnityEngine;
using UnityEngine.UI;

namespace Reveal.UI
{
    /// <summary>
    /// A short burst of falling confetti for the win screen — pure code, no
    /// particle assets. Spawns colored rounded chips that fall, spin and fade,
    /// then the emitter self-destructs.
    /// </summary>
    public class Confetti : MonoBehaviour
    {
        static readonly Color[] Palette =
        {
            new Color(1f, 0.84f, 0.4f), new Color(1f, 0.45f, 0.5f),
            new Color(0.5f, 0.85f, 1f), new Color(0.6f, 0.9f, 0.5f),
            new Color(0.75f, 0.6f, 1f),
        };

        public static void Burst(RectTransform parent, int count = 40)
        {
            var host = new GameObject("Confetti", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var hrt = host.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            var c = host.AddComponent<Confetti>();
            c.Spawn(count);
        }

        struct Chip { public RectTransform rt; public Vector2 vel; public float spin, life; }
        Chip[] _chips;
        float _t;

        void Spawn(int count)
        {
            _chips = new Chip[count];
            float w = ((RectTransform)transform).rect.width;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("c", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = Art.RoundedRect(6, false);
                img.type = Image.Type.Sliced;
                img.color = Palette[Random.Range(0, Palette.Length)];
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(Random.Range(14, 26), Random.Range(14, 26));
                rt.anchoredPosition = new Vector2(Random.Range(-w * 0.4f, w * 0.4f), Random.Range(300, 460));
                _chips[i] = new Chip
                {
                    rt = rt,
                    vel = new Vector2(Random.Range(-120f, 120f), Random.Range(-40f, -260f)),
                    spin = Random.Range(-260f, 260f),
                    life = Random.Range(1.1f, 1.7f),
                };
            }
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            bool anyAlive = false;
            for (int i = 0; i < _chips.Length; i++)
            {
                var ch = _chips[i];
                if (ch.rt == null) continue;
                if (_t > ch.life) { Destroy(ch.rt.gameObject); _chips[i].rt = null; continue; }
                anyAlive = true;
                ch.vel.y -= 520f * Time.unscaledDeltaTime; // gravity
                ch.rt.anchoredPosition += ch.vel * Time.unscaledDeltaTime;
                ch.rt.Rotate(0, 0, ch.spin * Time.unscaledDeltaTime);
                var img = ch.rt.GetComponent<Image>();
                var col = img.color; col.a = Mathf.Clamp01(1f - _t / ch.life); img.color = col;
            }
            if (!anyAlive) Destroy(gameObject);
        }
    }
}
