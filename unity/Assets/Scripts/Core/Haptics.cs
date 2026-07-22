using UnityEngine;

namespace Reveal.Core
{
    /// <summary>
    /// Lightweight device vibration for key moments (bomb hit, level win),
    /// gated by the player's haptics setting. Uses the built-in Handheld
    /// vibration on device; a no-op in the Editor.
    /// </summary>
    public static class Haptics
    {
        public static void Buzz()
        {
            if (!SaveSystem.HapticsOn) return;
#if UNITY_ANDROID || UNITY_IOS
            if (Application.isMobilePlatform) Handheld.Vibrate();
#endif
        }
    }
}
