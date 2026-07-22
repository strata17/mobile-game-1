using Reveal.Ads;
using Reveal.Audio;
using Reveal.Game;
using Reveal.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Reveal.Core
{
    /// <summary>
    /// Single entry point. Runs automatically before the first scene loads, so
    /// the game boots from an empty scene with zero manual wiring in the Editor.
    /// Constructs the camera, canvas, EventSystem, managers and UI in code.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var root = new GameObject("Reveal");
            Object.DontDestroyOnLoad(root);

            // Camera (solid dark background)
            var camGo = new GameObject("MainCamera", typeof(Camera));
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIFactory.Hex("#0b0c10");
            cam.orthographic = true;
            camGo.tag = "MainCamera";

            // EventSystem for UI input
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(root.transform, false);
            }

            // Canvas scaled for portrait phones (design at 1080x1920)
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // Vibrant full-screen gradient backdrop (behind all UI).
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bg = bgGo.GetComponent<RawImage>();
            bg.texture = Art.Gradient(UIFactory.Hex("#4a3aa8"), UIFactory.Hex("#140f30"));
            bg.raycastTarget = false;
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

            // Services
            var services = new GameObject("Services");
            services.transform.SetParent(root.transform, false);
            services.AddComponent<AdManager>();
            services.AddComponent<Sfx>();

            // UI + gameplay
            var ui = canvasGo.AddComponent<GameUI>();
            ui.Build(canvasGo.GetComponent<RectTransform>());

            var view = services.AddComponent<BoardView>();
            view.Setup(ui.BoardHost);

            var gm = services.AddComponent<GameManager>();
            gm.Init(ui, view);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
