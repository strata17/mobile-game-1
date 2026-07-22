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
        RawImage _gridOverlay;
        RectTransform _clueLayer;
        RectTransform _bombLayer;
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

            // Persistent per-cell grid lines, above the picture. Keeps every
            // cell's boundary visible even after its cover is revealed, so a
            // clue number always sits inside a clearly-bounded tile instead
            // of looking like it's floating over the artwork.
            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(RawImage));
            gridGo.transform.SetParent(_rt, false);
            _gridOverlay = gridGo.GetComponent<RawImage>();
            _gridOverlay.raycastTarget = false;
            UIFactory.Stretch(_gridOverlay.rectTransform);

            // Clue layer sits above the grid, below the covers.
            _clueLayer = UIFactory.Container(_rt, "Clues");
            UIFactory.Stretch(_clueLayer);

            // Bomb marks (drawn on hit / game-over reveal) get their own
            // layer so Load() can clear them between levels -- otherwise a
            // Continue (which reloads the board after defusing) would leave
            // stale bomb graphics behind from the previous attempt.
            _bombLayer = UIFactory.Container(_rt, "Bombs");
            UIFactory.Stretch(_bombLayer);

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

            _gridOverlay.texture = Art.GridTexture(board.Size, 64, new Color(1f, 1f, 1f, 0.16f));

            if (_covers != null)
                foreach (var c in _covers) if (c) Destroy(c.gameObject);

            foreach (Transform t in _clueLayer) Destroy(t.gameObject);
            foreach (Transform t in _bombLayer) Destroy(t.gameObject);

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
            // The gem itself is a neutral pearl (not scene-tinted): a fill that
            // matches the hidden picture's own hue tends to blend into it
            // (e.g. pink gems over a pink heart), killing contrast. A thin
            // scene-coloured rim still gives each level its own accent colour.
            var go = new GameObject($"C{r}_{c}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_rt, false);
            var rim = go.GetComponent<Image>();
            rim.sprite = Art.RoundedRect(20, false);
            rim.type = Image.Type.Sliced;
            rim.raycastTarget = false;
            rim.color = scene.BgTop;

            float gap = Mathf.Max(4f, _cell * 0.10f);
            var rt = rim.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // top-left origin
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cell - gap, _cell - gap);
            rt.anchoredPosition = CellCenter(r, c);

            var faceGo = new GameObject("face", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(go.transform, false);
            var face = faceGo.GetComponent<Image>();
            face.sprite = Art.RoundedRect(20, true);
            face.type = Image.Type.Sliced;
            face.raycastTarget = false;
            face.color = new Color(0.96f, 0.95f, 0.98f, 1f); // neutral pearl
            UIFactory.Stretch(face.rectTransform, _cell * 0.07f);

            return rim;
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
            disc.color = new Color(dc.r, dc.g, dc.b, 1f);
            var rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cell * 0.46f, _cell * 0.46f);
            rt.anchoredPosition = CellCenter(r, c);

            // Dark rim so the badge pops against any picture behind it.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Color txt = count == 1 ? new Color(0.35f, 0.24f, 0.05f) : Color.white;
            var t = UIFactory.Label(go.transform, "n", count.ToString(),
                Mathf.RoundToInt(_cell * 0.28f), txt, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(t.rectTransform);
            var textOutline = t.gameObject.AddComponent<Outline>();
            textOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            textOutline.effectDistance = new Vector2(1f, -1f);
        }

        /// <summary>
        /// Draws a bomb tile with an actual bomb glyph (dark round body, lit
        /// fuse, spark) — used both the instant a bomb is hit and for every
        /// remaining bomb revealed at game over, so a "revealed bomb" always
        /// shows what it was rather than an empty coloured square.
        /// </summary>
        public void ShowBombMark(int r, int c)
        {
            var go = new GameObject($"bomb{r}_{c}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_bombLayer, false);
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

            // Bomb body
            var bodyGo = new GameObject("body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(go.transform, false);
            var body = bodyGo.GetComponent<Image>();
            body.sprite = Art.RoundedRect(60, false);
            body.type = Image.Type.Sliced;
            body.raycastTarget = false;
            body.color = UIFactory.Hex("#20222f");
            UIFactory.Stretch(body.rectTransform, _cell * 0.16f);

            // Gloss highlight
            var glossGo = new GameObject("gloss", typeof(RectTransform), typeof(Image));
            glossGo.transform.SetParent(bodyGo.transform, false);
            var gloss = glossGo.GetComponent<Image>();
            gloss.sprite = Art.RoundedRect(60, false);
            gloss.type = Image.Type.Sliced;
            gloss.raycastTarget = false;
            gloss.color = new Color(1f, 1f, 1f, 0.35f);
            var grt = gloss.rectTransform;
            grt.anchorMin = new Vector2(0.18f, 0.55f); grt.anchorMax = new Vector2(0.45f, 0.8f);
            grt.offsetMin = grt.offsetMax = Vector2.zero;

            // Fuse (thin bar angled up-right from the body)
            var fuseGo = new GameObject("fuse", typeof(RectTransform), typeof(Image));
            fuseGo.transform.SetParent(go.transform, false);
            var fuse = fuseGo.GetComponent<Image>();
            fuse.color = UIFactory.Hex("#7a5a3a");
            fuse.raycastTarget = false;
            var frt = fuse.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.68f, 0.78f);
            frt.pivot = new Vector2(0f, 0f);
            frt.sizeDelta = new Vector2(_cell * 0.32f, _cell * 0.07f);
            frt.localRotation = Quaternion.Euler(0, 0, 40f);

            // Spark at the fuse tip
            var sparkGo = new GameObject("spark", typeof(RectTransform), typeof(Image));
            sparkGo.transform.SetParent(go.transform, false);
            var spark = sparkGo.GetComponent<Image>();
            spark.sprite = Art.RoundedRect(30, true);
            spark.type = Image.Type.Sliced;
            spark.raycastTarget = false;
            spark.color = UIFactory.Hex("#ffcb47");
            var srt = spark.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(_cell * 0.30f, -_cell * 0.05f);
            srt.sizeDelta = new Vector2(_cell * 0.18f, _cell * 0.18f);
        }

        /// <summary>Show every bomb (used on game over so the player sees the truth).</summary>
        public void RevealBombs()
        {
            if (_board == null) return;
            for (int r = 0; r < _board.Size; r++)
                for (int c = 0; c < _board.Size; c++)
                    if (_board.Bomb[r, c])
                    {
                        // Still-covered bombs need their cover cleared first,
                        // otherwise the mark would be drawn underneath it.
                        if (_covers[r, c] != null)
                        {
                            Destroy(_covers[r, c].gameObject);
                            _covers[r, c] = null;
                        }
                        ShowBombMark(r, c);
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
