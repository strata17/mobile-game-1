#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Reveal.EditorTools
{
    /// <summary>
    /// Configures Player Settings for a store build in code, so the project is
    /// ship-ready without hand-editing ProjectSettings YAML. Runs once on load
    /// and is re-runnable from the "Reveal" menu.
    /// </summary>
    [InitializeOnLoad]
    public static class RevealBuildSetup
    {
        const string ProductName = "Reveal";
        const string CompanyName = "Reveal Games";
        const string BundleId = "com.revealgames.reveal";

        static RevealBuildSetup()
        {
            // Apply once (detect a fresh/unconfigured project by product name).
            if (PlayerSettings.productName != ProductName)
                EditorApplication.delayCall += Apply;
        }

        [MenuItem("Reveal/Configure Player Settings")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            // Portrait, one-handed casual game.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            ApplyIconIfPresent();

            AssetDatabase.SaveAssets();
            Debug.Log("[Reveal] Player Settings configured for store build.");
        }

        static void ApplyIconIfPresent()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/icon.png");
            if (icon == null) return;
            var icons = new[] { icon };
            try
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, icons);
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, icons);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Reveal] Could not set app icon automatically; set it in Player Settings. " + e.Message);
            }
        }
    }
}
#endif
