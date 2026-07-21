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
