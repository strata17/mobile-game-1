using System;
using Reveal.Core;
using Reveal.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Reveal.Game
{
    /// <summary>
    /// Renders a Board: the hidden picture underneath, a grid of "gem" cover
    /// tiles on top, and drag-to-scratch input. Reveal logic lives in Board;
    /// this class is purely presentation + input, reporting each scratched cell
    /// back to the owner via OnReveal.
    /// </summary>
    public class BoardView : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<int, int> OnReveal;

        Board _board;
        RectTransform _rt;
        RawImage _picture;
        RectTransform _clueLayer;
        Image[,] _covers;
        float _cell;

        public void Setup(RectTransform parent)
        {
            _rt = UIFactory.Container(parent, "BoardView");
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);

            // Round the whole board and clip children to it (rounded picture +
            // tiles). The mask graphic doubles as the board's backing colour.
            var frame = _rt.gameObject.AddComponent<Image>();
            frame.sprite = Art.RoundedRect(40, false);
            frame.type = Image.Type.Sliced;
            frame.color = UIFactory.Hex("#0e1024");
            var mask = _rt.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var picGo = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
            picGo.transform.SetParent(_rt, false);
            _picture = picGo.GetComponent<RawImage>();
            UIFactory.Stretch(_picture.rectTransform);
            _picture.raycastTarget = false;

            // Clue layer sits above the picture, below the covers.
            _clueLayer = UIFactory.Container(_rt, "Clues");
            UIFactory.Stretch(_clueLayer);

            // Transparent raycast catcher for drag input across the whole board.
            var input = new GameObject("Input", typeof(RectTransform), typeof(Image));
            input.transform.SetParent(_rt, false);
            var img = input.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            UIFactory.Stretch(img.rectTransform);
            input.AddComponent<DragProxy>().Target = this;
        }

        bool _shadowAdded;

        public void Load(Board board, Texture2D picture, float sizePx)
        {
            _board = board;
            _rt.sizeDelta = new Vector2(sizePx, sizePx);
            if (!_shadowAdded) { Art.AddShadow(_rt, 40f, -16f); _shadowAdded = true; }
            _picture.texture = picture;
            // Center-crop the picture to a square so wide (16:9) or tall art
            // fills the board without distortion.
            float aw = picture.width, ah = picture.height;
            if (aw > ah) { float u = ah / aw; _picture.uvRect = new Rect((1f - u) / 2f, 0f, u, 1f); }
            else if (ah > aw) { float v = aw / ah; _picture.uvRect = new Rect(0f, (1f - v) / 2f, 1f, v); }
            else _picture.uvRect = new Rect(0f, 0f, 1f, 1f);
            _cell = sizePx / board.Size;

            if (_covers != null)
                foreach (var c in _covers) if (c) Destroy(c.gameObject);

            foreach (Transform t in _clueLayer) Destroy(t.gameObject);

            _covers = new Image[board.Size, board.Size];
            var scene = Scenes.ForLevel(board.Level);
            for (int r = 0; r < board.Size; r++)
                for (int c = 0; c < board.Size; c++)
                    _covers[r, c] = MakeCover(r, c, scene, board.Bomb[r, c]);

            RefreshAll();

            // Seed clues on the endowed (pre-revealed) safe tiles.
            for (int r = 0; r < board.Size; r++)
                for (int c = 0; c < board.Size; c++)
                    if (board.Revealed[r, c] && !board.Bomb[r, c])
                        ShowClue(r, c, board.Adj[r, c]);
        }

        Image MakeCover(int r, int c, Scene scene, bool bomb)
        {
            // Bombs are hidden now — every cover is an identical glossy gem, so
            // danger is deduced from the revealed clues, not seen on the cover.
            var go = new GameObject($"C{r}_{c}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_rt, false);
            var img = go.GetComponent<Image>();
            img.sprite = Art.RoundedRect(20, true);  // rounded + glossy gem
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = Color.Lerp(scene.BgTop, Color.white, 0.28f);

            float gap = Mathf.Max(4f, _cell * 0.10f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // top-left origin
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cell - gap, _cell - gap);
            rt.anchoredPosition = CellCenter(r, c);
            return img;
        }

        static readonly Color[] DangerColors =
        {
            default,                          // 0 (unused)
            new Color(1f, 0.85f, 0.30f),      // 1 - soft amber
            new Color(1f, 0.62f, 0.24f),      // 2 - orange
            new Color(1f, 0.37f, 0.42f),      // 3+ - red
        };

        /// <summary>
        /// Reveal a Minesweeper-style clue on a safe cell: a small heat-coloured
        /// disc with the adjacent-bomb count. Count 0 shows nothing so the
        /// picture stays clean.
        /// </summary>
        public void ShowClue(int r, int c, int count)
        {
            if (count <= 0 || _clueLayer == null) return;
            Color dc = DangerColors[Mathf.Min(count, 3)];

            var go = new GameObject($"clue{r}_{c}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_clueLayer, false);
            var disc = go.GetComponent<Image>();
            disc.sprite = Art.RoundedRect(40, true);
            disc.type = Image.Type.Sliced;
            disc.raycastTarget = false;
            disc.color = new Color(dc.r, dc.g, dc.b, 0.92f);
            var rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cell * 0.5f, _cell * 0.5f);
            rt.anchoredPosition = CellCenter(r, c);

            Color txt = count == 1 ? new Color(0.35f, 0.24f, 0.05f) : Color.white;
            var t = UIFactory.Label(go.transform, "n", count.ToString(),
                Mathf.RoundToInt(_cell * 0.34f), txt, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(t.rectTransform);
        }

        /// <summary>Show every bomb (used on game over so the player sees the truth).</summary>
        public void RevealBombs()
        {
            if (_board == null) return;
            for (int r = 0; r < _board.Size; r++)
                for (int c = 0; c < _board.Size; c++)
                    if (_board.Bomb[r, c])
                    {
                        var go = new GameObject($"bomb{r}_{c}", typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(_rt, false);
                        var img = go.GetComponent<Image>();
                        img.sprite = Art.RoundedRect(20, true);
                        img.type = Image.Type.Sliced;
                        img.raycastTarget = false;
                        img.color = new Color(1f, 0.30f, 0.38f, 0.95f);
                        var rt = img.rectTransform;
                        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        float gap = Mathf.Max(4f, _cell * 0.10f);
                        rt.sizeDelta = new Vector2(_cell - gap, _cell - gap);
                        rt.anchoredPosition = CellCenter(r, c);
                    }
        }

        Vector2 CellCenter(int r, int c)
        {
            // Anchored to top-left of the board; y grows downward.
            float x = (c + 0.5f) * _cell;
            float y = -(r + 0.5f) * _cell;
            return new Vector2(x, y);
        }

        public void RefreshAll()
        {
            for (int r = 0; r < _board.Size; r++)
                for (int c = 0; c < _board.Size; c++)
                    if (_board.Revealed[r, c] && _covers[r, c] != null)
                    {
                        Destroy(_covers[r, c].gameObject);
                        _covers[r, c] = null;
                    }
        }

        public void RevealTile(int r, int c)
        {
            if (_covers != null && _covers[r, c] != null)
            {
                _covers[r, c].raycastTarget = false;
                _covers[r, c].gameObject.AddComponent<Reveal.UI.Vanish>(); // pop, then self-destruct
                _covers[r, c] = null;
            }
        }

        // ---------- input ----------
        public void OnPointerDown(PointerEventData e) => ScratchAt(e);
        public void OnDrag(PointerEventData e) => ScratchAt(e);

        void ScratchAt(PointerEventData e)
        {
            if (_board == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt, e.position, e.pressEventCamera, out Vector2 local);

            // Convert from center-pivot local space to top-left cell indices.
            float px = local.x + _rt.sizeDelta.x * 0.5f;
            float py = _rt.sizeDelta.y * 0.5f - local.y;
            int c = Mathf.FloorToInt(px / _cell);
            int r = Mathf.FloorToInt(py / _cell);
            if (r < 0 || c < 0 || r >= _board.Size || c >= _board.Size) return;

            OnReveal?.Invoke(r, c);
        }

        /// <summary>Forwards drag events from the transparent input layer.</summary>
        class DragProxy : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            public BoardView Target;
            public void OnPointerDown(PointerEventData e) => Target.OnPointerDown(e);
            public void OnDrag(PointerEventData e) => Target.OnDrag(e);
        }
    }
}
