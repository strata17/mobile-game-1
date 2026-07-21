using UnityEngine;

namespace Reveal.Core
{
    /// <summary>A hidden picture and its background gradient.</summary>
    public struct Scene
    {
        public string Motif;
        public Color BgTop;
        public Color BgBottom;

        public Scene(string motif, string top, string bottom)
        {
            Motif = motif;
            BgTop = Hex(top);
            BgBottom = Hex(bottom);
        }

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }
    }

    public static class Scenes
    {
        public static readonly Scene[] All =
        {
            new Scene("sun",     "#ffd76a", "#ff8f3d"),
            new Scene("star",    "#8f7bff", "#4d3fd0"),
            new Scene("heart",   "#ff8fab", "#ff5f7e"),
            new Scene("diamond", "#5fdcea", "#2f8fd6"),
            new Scene("flower",  "#9be86e", "#34a866"),
            new Scene("moon",    "#46579c", "#232a5c"),
            new Scene("rocket",  "#8fb0ff", "#5566e6"),
            new Scene("balloon", "#ffa06e", "#ff6f91"),
            new Scene("rainbow", "#79ccff", "#3a8fe0"),
            new Scene("cloud",   "#84bbff", "#4f8fe0"),
            new Scene("planet",  "#6a5fe0", "#3a2f8c"),
            new Scene("bolt",    "#ffd76a", "#ffa93d"),
        };

        public static int Count => All.Length;

        public static Scene ForLevel(int level)
        {
            return All[(level - 1) % All.Length];
        }
    }
}
