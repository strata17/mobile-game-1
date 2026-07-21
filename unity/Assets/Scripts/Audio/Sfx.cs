using Reveal.Core;
using UnityEngine;

namespace Reveal.Audio
{
    /// <summary>
    /// Tiny procedural sound engine — generates short tone clips at runtime so
    /// there are no audio assets to import. Mirrors the WebAudio blips of the
    /// prototype (scratch, bonus, win, bomb, coin).
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }
        AudioSource _src;

        void Awake()
        {
            Instance = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
        }

        public void Scratch() => Play(Tone(320f, 0.05f, 0.25f));
        public void Bonus()   => Play(Chord(new[] { 523f, 784f }, 0.18f, 0.3f));
        public void Coin()    => Play(Tone(880f, 0.08f, 0.3f));
        public void Win()     => Play(Chord(new[] { 523f, 659f, 784f, 1046f }, 0.4f, 0.35f));
        public void Bomb()    => Play(Tone(90f, 0.3f, 0.4f, true));

        void Play(AudioClip clip)
        {
            if (!SaveSystem.SoundOn || clip == null) return;
            _src.PlayOneShot(clip);
        }

        static AudioClip Tone(float freq, float dur, float vol, bool noise = false)
        {
            int rate = 44100;
            int n = Mathf.CeilToInt(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 12f);
                float s = noise ? (Random.value * 2f - 1f) : Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = s * env * vol;
            }
            var clip = AudioClip.Create("t", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(float[] freqs, float dur, float vol)
        {
            int rate = 44100;
            int n = Mathf.CeilToInt(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 5f);
                float s = 0f;
                foreach (var f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = s / freqs.Length * env * vol;
            }
            var clip = AudioClip.Create("c", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
