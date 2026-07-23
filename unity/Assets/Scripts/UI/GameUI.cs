using System;
using System.Collections.Generic;
using Reveal.Core;
using Reveal.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Reveal.UI
{
    /// <summary>
    /// Builds and drives every screen: top HUD, in-level progress/hearts, the
    /// board host, and the menu / level-complete / game-over / settings
    /// overlays. GameManager wires the button callbacks and calls the Show/Set
    /// methods; this class owns no game logic.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // Callbacks (wired by GameManager)
        public Action OnPlay, OnNextLevel, OnRetry, OnDaily, OnHint, OnContinueAd, OnSettings, OnCloseSettings, OnReset, OnToggleSound, OnHome;

        // HUD
        Text _coins, _level, _score, _best, _progressPct;
        Image _progressFill;
        RectTransform _heartsRow;
        RectTransform _boardHost;

        // Overlays
        RectTransform _menu, _levelComplete, _gameOver, _settings;
        Text _lcTitle, _lcLevel, _lcPoints, _lcCoins, _unlockNote, _starText;
        RectTransform _starRow, _chestBadge;
        Image[] _starImages;
        Text _goScore, _nearMiss;
        Text _chapterLabel, _streakLabel, _collectionCount, _menuCoins, _bombInfo;
        RectTransform _streakRow;
        Image _journeyFill;
        Button _dailyBtn;
        RectTransform _missionsList, _collectionRow;
        Button _hintBtn;
        GameObject _hintAdTag;

        public RectTransform BoardHost => _boardHost;

        readonly Color _cardBg = UIFactory.Hex("#141726");
        readonly Color _accent = UIFactory.Hex("#ff8f3d");
        readonly Color _primary = UIFactory.Hex("#5f6dff");
        readonly Color _text = UIFactory.Hex("#f4f6ff");
        readonly Color _muted = UIFactory.Hex("#9aa3c7");

        RectTransform _root;
        RectTransform _chrome;

        public void Build(RectTransform root)
        {
            _root = root;
            // In-game chrome lives inside the safe area; full-screen overlays don't.
            _chrome = UIFactory.Container(_root, "Chrome");
            UIFactory.Stretch(_chrome);
            _chrome.gameObject.AddComponent<SafeArea>();

            BuildHud();
            BuildBoardHost();
            BuildControls();
            BuildTutorial();
            BuildMenu();
            BuildLevelComplete();
            BuildGameOver();
            BuildSettings();
        }

        // ---------------- HUD ----------------
        void BuildHud()
        {
            var top = UIFactory.Container(_chrome, "TopBar");
            top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 120);
            top.anchoredPosition = new Vector2(0, -20);

            var coinPill = UIFactory.RoundedPanel(top, "CoinPill", _cardBg, 32, true).rectTransform;
            UIFactory.Anchor(coinPill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(210, 70));
            IconOn(coinPill, GameArt.Coin, new Vector2(0, 0.5f), new Vector2(20, 0), 44);
            _coins = UIFactory.Label(coinPill, "Coins", "0", 36, UIFactory.Hex("#ffd76a"), TextAnchor.MiddleRight, FontStyle.Bold);
            _coins.rectTransform.anchorMin = new Vector2(0, 0); _coins.rectTransform.anchorMax = new Vector2(1, 1);
            _coins.rectTransform.offsetMin = new Vector2(56, 0); _coins.rectTransform.offsetMax = new Vector2(-20, 0);

            var gear = UIFactory.RoundedPanel(top, "Gear", _cardBg, 26, true);
            var gearRt = gear.rectTransform;
            UIFactory.Anchor(gearRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(72, 72));
            gear.gameObject.AddComponent<Button>().onClick.AddListener(() => OnSettings?.Invoke());
            gear.gameObject.AddComponent<PressPop>();
            if (GameArt.Gear != null)
            {
                var gi = IconOn(gearRt, GameArt.Gear, new Vector2(0.5f, 0.5f), Vector2.zero, 40);
                gi.rectTransform.anchorMin = gi.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
            else
            {
                var gt = UIFactory.Label(gear.transform, "L", "•••", 34, _text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(gt.rectTransform);
            }

            // Frosted backing behind stats/progress/hearts so that text and
            // icons keep contrast regardless of the busy sky artwork behind
            // them -- without this, light-grey labels directly over a warm,
            // bright background become nearly unreadable.
            var hudBg = UIFactory.RoundedPanel(_chrome, "HudBg", new Color(_cardBg.r, _cardBg.g, _cardBg.b, 0.85f), 36).rectTransform;
            hudBg.anchorMin = new Vector2(0, 1); hudBg.anchorMax = new Vector2(1, 1);
            hudBg.pivot = new Vector2(0.5f, 1);
            hudBg.sizeDelta = new Vector2(-60, 300);
            hudBg.anchoredPosition = new Vector2(0, -125);

            // Stats row
            var stats = UIFactory.Container(_chrome, "Stats");
            stats.anchorMin = new Vector2(0, 1); stats.anchorMax = new Vector2(1, 1);
            stats.pivot = new Vector2(0.5f, 1);
            stats.sizeDelta = new Vector2(0, 120);
            stats.anchoredPosition = new Vector2(0, -150);
            _level = Stat(stats, "LEVEL", -0.32f, out _);
            _score = Stat(stats, "SCORE", 0f, out _);
            _best = Stat(stats, "BEST", 0.32f, out _);

            // Progress
            var prow = UIFactory.Container(_chrome, "Progress");
            prow.anchorMin = new Vector2(0, 1); prow.anchorMax = new Vector2(1, 1);
            prow.pivot = new Vector2(0.5f, 1);
            prow.sizeDelta = new Vector2(-80, 40);
            prow.anchoredPosition = new Vector2(0, -300);
            var trackImg = UIFactory.RoundedPanel(prow, "Track", UIFactory.Hex("#0c0e18"), 18);
            var track = trackImg.rectTransform;
            UIFactory.Stretch(track);
            var trackEdge = trackImg.gameObject.AddComponent<Outline>();
            trackEdge.effectColor = new Color(1f, 1f, 1f, 0.18f);
            trackEdge.effectDistance = new Vector2(1f, -1f);
            _progressFill = UIFactory.RoundedPanel(track, "Fill", _accent, 18, true);
            _progressFill.rectTransform.anchorMin = new Vector2(0, 0);
            _progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            _progressFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _progressFill.rectTransform.sizeDelta = new Vector2(0, 0);
            UIFactory.AddGloss(_progressFill.transform, 0.3f, 1f, 0.08f);
            _progressPct = UIFactory.Label(prow, "Pct", "0%", 26, _text);
            UIFactory.Stretch(_progressPct.rectTransform);

            _bombInfo = UIFactory.Label(_chrome, "BombInfo", "", 24, _muted);
            _bombInfo.rectTransform.anchorMin = new Vector2(0.5f, 1);
            _bombInfo.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _bombInfo.rectTransform.pivot = new Vector2(0.5f, 1);
            _bombInfo.rectTransform.sizeDelta = new Vector2(500, 32);
            _bombInfo.rectTransform.anchoredPosition = new Vector2(0, -388);

            _heartsRow = UIFactory.Container(_chrome, "Hearts");
            _heartsRow.anchorMin = new Vector2(0.5f, 1); _heartsRow.anchorMax = new Vector2(0.5f, 1);
            _heartsRow.pivot = new Vector2(0.5f, 1);
            _heartsRow.sizeDelta = new Vector2(300, 50);
            _heartsRow.anchoredPosition = new Vector2(0, -350);
            var hl = _heartsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 8;
        }

        Text Stat(RectTransform parent, string label, float xAnchor, out Text lbl)
        {
            var box = UIFactory.Container(parent, label);
            box.anchorMin = box.anchorMax = new Vector2(0.5f + xAnchor, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(200, 110);
            lbl = UIFactory.Label(box, "L", label, 22, _muted, TextAnchor.UpperCenter);
            UIFactory.Stretch(lbl.rectTransform);
            var val = UIFactory.Label(box, "V", "0", 52, _text, TextAnchor.LowerCenter, FontStyle.Bold);
            UIFactory.Stretch(val.rectTransform);
            return val;
        }

        void BuildBoardHost()
        {
            _boardHost = UIFactory.Container(_chrome, "BoardHost");
            _boardHost.anchorMin = new Vector2(0.5f, 0.5f);
            _boardHost.anchorMax = new Vector2(0.5f, 0.5f);
            _boardHost.pivot = new Vector2(0.5f, 0.5f);
            _boardHost.anchoredPosition = new Vector2(0, -40);
            _boardHost.sizeDelta = new Vector2(980, 980);
        }

        RectTransform _tutHint;

        void BuildTutorial()
        {
            _tutHint = UIFactory.RoundedPanel(_chrome, "TutHint", new Color(0.02f, 0.03f, 0.06f, 0.82f), 26).rectTransform;
            _tutHint.anchorMin = _tutHint.anchorMax = new Vector2(0.5f, 0.5f);
            _tutHint.pivot = new Vector2(0.5f, 0.5f);
            _tutHint.anchoredPosition = new Vector2(0, -40);
            _tutHint.sizeDelta = new Vector2(680, 96);
            var t = UIFactory.Label(_tutHint, "t", "Drag to scratch · numbers warn of bombs nearby", 26, _text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(t.rectTransform);
            _tutHint.gameObject.AddComponent<Pulse>();
            _tutHint.gameObject.SetActive(false);
        }

        public void ShowTutorial(bool show)
        {
            if (_tutHint != null) _tutHint.gameObject.SetActive(show);
        }

        void BuildControls()
        {
            _hintBtn = UIFactory.Button(_chrome, "HintBtn", "Reveal a safe tile", _cardBg, _text, 32);
            var rt = (RectTransform)_hintBtn.transform;
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 60);
            rt.sizeDelta = new Vector2(640, 96);
            _hintBtn.onClick.AddListener(() => OnHint?.Invoke());

            if (GameArt.Magnify != null)
            {
                var mi = new GameObject("Magnify", typeof(RectTransform), typeof(Image));
                mi.transform.SetParent(_hintBtn.transform, false);
                var mimg = mi.GetComponent<Image>();
                mimg.sprite = GameArt.SpriteFrom(GameArt.Magnify);
                mimg.preserveAspect = true;
                mimg.raycastTarget = false;
                var mrt = mi.GetComponent<RectTransform>();
                mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
                mrt.pivot = new Vector2(0f, 0.5f);
                mrt.anchoredPosition = new Vector2(16, 0);
                mrt.sizeDelta = new Vector2(56, 56);
            }

            if (GameArt.AdTag != null)
            {
                var ai = new GameObject("AdTag", typeof(RectTransform), typeof(Image));
                ai.transform.SetParent(_hintBtn.transform, false);
                var aimg = ai.GetComponent<Image>();
                aimg.sprite = GameArt.SpriteFrom(GameArt.AdTag);
                aimg.preserveAspect = true;
                aimg.raycastTarget = false;
                var art = ai.GetComponent<RectTransform>();
                art.anchorMin = art.anchorMax = new Vector2(1f, 0.5f);
                art.pivot = new Vector2(1f, 0.5f);
                art.anchoredPosition = new Vector2(-16, 0);
                art.sizeDelta = new Vector2(48, 48);
                _hintAdTag = ai;
            }
        }

        // ---------------- overlays ----------------
        RectTransform Overlay(string name, out RectTransform card, bool opaque = false, float scrimAlpha = 0.88f)
        {
            RectTransform ov;
            if (opaque)
            {
                // Full opaque gradient — a real screen, nothing bleeds behind it.
                var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(_root, false);
                // Flat procedural gradient for every full-screen backdrop --
                // deliberately NOT the painterly rendered sky/attic
                // textures (GameArt.Background / MenuBackground). Those are
                // photoreal, everything else on screen (icons, buttons,
                // cards) is flat vector "toy" art; putting a painted photo
                // full-bleed behind flat UI is the single biggest source of
                // the "doesn't look right" feeling across every screen, not
                // just the card. One flat gradient language, everywhere.
                var ri = go.GetComponent<RawImage>();
                ri.texture = Art.Gradient(Theme.BgTop, Theme.BgBottom);
                ri.uvRect = new Rect(0, 0, 1, 1);
                ov = go.GetComponent<RectTransform>();
            }
            else
            {
                // A high-alpha near-black scrim so the modal reads cleanly
                // against gameplay behind it. A translucent tint alone
                // blends with a warm/bright background into a muddy brown
                // wash rather than a clean darkened backdrop.
                var scrim = Theme.Scrim; scrim.a = scrimAlpha;
                ov = UIFactory.Panel(_root, name, scrim);
            }
            UIFactory.Stretch(ov);

            card = UIFactory.RoundedPanel(ov, "Card", _cardBg, Theme.RadiusCard).rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(920, 0);

            // A real, clearly-visible drop shadow so the card reads as a
            // floating surface. A CHILD (not a fixed-size sibling) so it
            // tracks the card's actual size at all times -- the card's
            // height comes from ContentSizeFitter and isn't known yet at
            // this point in Build(), so anything that snapshots sizeDelta
            // here would end up the wrong size once the fitter resolves.
            var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadowGo.transform.SetParent(card, false);
            shadowGo.transform.SetAsFirstSibling();
            var shadowImg = shadowGo.GetComponent<Image>();
            shadowImg.sprite = Art.SoftShadow(Theme.ShadowRadiusCard, Theme.ShadowAlphaCard);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.raycastTarget = false;
            var shadowRt = shadowGo.GetComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero; shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(-10, -22); shadowRt.offsetMax = new Vector2(10, 6);
            shadowGo.AddComponent<LayoutElement>().ignoreLayout = true;

            // Clipping lives on its own child, separate from the card's own
            // Image (which still carries the Shadow/Outline-free rounded
            // shape as a solid-colour fallback). Combining a Mask with
            // Shadow/Outline BaseMeshEffects on the SAME GameObject is a
            // known source of stencil/mesh glitches in Unity UI (observed
            // here as the card's rounded corners rendering square) --
            // isolating the mask onto its own object avoids that entirely.
            var clipGo = new GameObject("ClipFrame", typeof(RectTransform), typeof(Image), typeof(Mask));
            clipGo.transform.SetParent(card, false);
            var clipImg = clipGo.GetComponent<Image>();
            clipImg.sprite = Art.RoundedRect(Theme.RadiusCard, false);
            clipImg.type = Image.Type.Sliced;
            clipImg.color = _cardBg;
            clipGo.GetComponent<Mask>().showMaskGraphic = true;
            var clipRt = clipGo.GetComponent<RectTransform>();
            UIFactory.Stretch(clipRt);
            clipGo.AddComponent<LayoutElement>().ignoreLayout = true;

            // Hairline light edge on its OWN object, not clipGo -- putting
            // Outline on the same GameObject as a Mask is exactly the
            // pattern that caused the square-corner bug in the first place,
            // just relocated. This object has an invisible fill (alpha 0)
            // purely so Outline has geometry to duplicate; only the
            // duplicated, offset copies show.
            var edgeGo = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            edgeGo.transform.SetParent(card, false);
            var edgeImg = edgeGo.GetComponent<Image>();
            edgeImg.sprite = Art.RoundedRect(Theme.RadiusCard, false);
            edgeImg.type = Image.Type.Sliced;
            edgeImg.color = new Color(0f, 0f, 0f, 0f);
            edgeImg.raycastTarget = false;
            UIFactory.Stretch(edgeGo.GetComponent<RectTransform>());
            edgeGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var cardEdge = edgeGo.AddComponent<Outline>();
            cardEdge.effectColor = new Color(1f, 1f, 1f, 0.10f);
            cardEdge.effectDistance = new Vector2(1.5f, 1.5f);

            // Themed gradient fill instead of a flat near-black panel, which
            // read as a generic dark-mode dialog clashing with the vibrant
            // coral/purple palette everywhere else.
            var cardGradGo = new GameObject("Gradient", typeof(RectTransform), typeof(RawImage));
            cardGradGo.transform.SetParent(clipGo.transform, false);
            var cardGrad = cardGradGo.GetComponent<RawImage>();
            cardGrad.texture = Art.Gradient(Theme.CardTop, Theme.CardBottom);
            cardGrad.raycastTarget = false;
            UIFactory.Stretch(cardGrad.rectTransform);

            // NOTE: deliberately NOT layering the ornate parchment texture
            // (GameArt.CardBackground) here. It's a painterly/photoreal
            // asset and everything else in this UI -- icons, tiles, buttons
            // -- is flat glossy "toy" art. Layering a richly-textured
            // painted surface behind that is exactly the "mixing flat and
            // skeuomorphic" anti-pattern that was making every screen feel
            // incoherent regardless of how any single element was tuned.
            // The plain gradient above is the card surface, full stop.

            var vg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.childAlignment = TextAnchor.UpperCenter;
            vg.spacing = 24; vg.padding = new RectOffset(56, 56, 56, 56);
            vg.childControlHeight = false; vg.childControlWidth = true;
            vg.childForceExpandHeight = false; vg.childForceExpandWidth = true;

            // Auto-size the card height to fit its content (no dead space).
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return ov;
        }

        /// <summary>A small square icon anchored at a point (e.g. inside a pill/button).</summary>
        Image IconOn(RectTransform parent, Texture2D tex, Vector2 anchor, Vector2 offset, float size)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            if (tex != null) { img.sprite = GameArt.SpriteFrom(tex); img.preserveAspect = true; }
            else img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>
        /// A round danger badge with an illustrated bomb glyph (dark body,
        /// gloss, lit fuse + spark) — used on the game-over screen instead
        /// of a plain "!" character, matching the bomb marks on the board.
        /// </summary>
        void BuildBombBadge(Transform parent, float size)
        {
            var wrap = UIFactory.Container(parent, "BombBadge");
            wrap.sizeDelta = new Vector2(0, size + 20);

            var back = UIFactory.RoundedPanel(wrap, "back", UIFactory.Hex("#ff5f7e"), Mathf.RoundToInt(size * 0.5f), true).rectTransform;
            back.anchorMin = back.anchorMax = new Vector2(0.5f, 0.5f);
            back.sizeDelta = new Vector2(size, size);

            var body = UIFactory.RoundedPanel(back, "body", UIFactory.Hex("#20222f"), 60).rectTransform;
            UIFactory.Stretch(body, size * 0.16f);

            var gloss = UIFactory.RoundedPanel(body, "gloss", new Color(1f, 1f, 1f, 0.35f), 60).rectTransform;
            gloss.anchorMin = new Vector2(0.18f, 0.55f); gloss.anchorMax = new Vector2(0.45f, 0.8f);
            gloss.offsetMin = gloss.offsetMax = Vector2.zero;

            var fuse = new GameObject("fuse", typeof(RectTransform), typeof(Image));
            fuse.transform.SetParent(back, false);
            fuse.GetComponent<Image>().color = UIFactory.Hex("#7a5a3a");
            var frt = fuse.GetComponent<RectTransform>();
            frt.anchorMin = frt.anchorMax = new Vector2(0.68f, 0.78f);
            frt.pivot = new Vector2(0f, 0f);
            frt.sizeDelta = new Vector2(size * 0.32f, size * 0.07f);
            frt.localRotation = Quaternion.Euler(0, 0, 40f);

            var spark = UIFactory.RoundedPanel(back, "spark", UIFactory.Hex("#ffcb47"), 30, true).rectTransform;
            spark.anchorMin = spark.anchorMax = new Vector2(0.5f, 1f);
            spark.pivot = new Vector2(0.5f, 0.5f);
            spark.anchoredPosition = new Vector2(size * 0.30f, -size * 0.05f);
            spark.sizeDelta = new Vector2(size * 0.18f, size * 0.18f);
        }

        /// <summary>An aspect-preserving image row (fixed height, full width).</summary>
        Image ImageFit(Transform parent, string name, Texture2D tex, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = GameArt.SpriteFrom(tex);
            img.preserveAspect = true;
            img.raycastTarget = false;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            return img;
        }

        /// <summary>Absolutely-placed, aspect-preserving hero image on the background.</summary>
        void HeroImage(Transform parent, string name, Texture2D tex, float centerFromTop, float boxW, float boxH)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = GameArt.SpriteFrom(tex);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -centerFromTop);
            rt.sizeDelta = new Vector2(boxW, boxH);
        }

        void BuildMenu()
        {
            _menu = Overlay("Menu", out var card, opaque: true);

            // Card: a slim gradient panel in the lower two-thirds; the hero
            // art (mascot + logo + coin) sits on the background above it.
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0, -620);
            card.sizeDelta = new Vector2(940, 0);

            // Hero art on the background
            var coinPill = UIFactory.RoundedPanel(_menu, "MenuCoins", UIFactory.Hex("#0c0e18"), 34, true).rectTransform;
            coinPill.anchorMin = coinPill.anchorMax = new Vector2(1, 1);
            coinPill.pivot = new Vector2(1, 1);
            coinPill.anchoredPosition = new Vector2(-30, -36);
            coinPill.sizeDelta = new Vector2(230, 74);
            IconOn(coinPill, GameArt.Coin, new Vector2(0, 0.5f), new Vector2(18, 0), 46);
            _menuCoins = UIFactory.Label(coinPill, "c", "0", 32, UIFactory.Hex("#ffd76a"), TextAnchor.MiddleRight, FontStyle.Bold);
            _menuCoins.rectTransform.anchorMin = new Vector2(0, 0); _menuCoins.rectTransform.anchorMax = new Vector2(1, 1);
            _menuCoins.rectTransform.offsetMin = new Vector2(58, 0); _menuCoins.rectTransform.offsetMax = new Vector2(-18, 0);

            if (GameArt.Mascot != null) HeroImage(_menu, "Mascot", GameArt.Mascot, 245, 600, 330);

            if (GameArt.Logo != null)
                HeroImage(_menu, "Logo", GameArt.Logo, 515, 640, 190);
            else
                UIFactory.Label(_menu, "Title", "REVEAL", 110, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.anchoredPosition = new Vector2(0, 640);

            _streakRow = UIFactory.Container(card, "StreakRow");
            _streakRow.sizeDelta = new Vector2(0, 44);
            var streakInner = UIFactory.Container(_streakRow, "StreakInner");
            streakInner.anchorMin = streakInner.anchorMax = new Vector2(0.5f, 0.5f);
            streakInner.pivot = new Vector2(0.5f, 0.5f);
            var streakLayout = streakInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            streakLayout.childAlignment = TextAnchor.MiddleCenter; streakLayout.spacing = 8;
            streakLayout.childControlWidth = false; streakLayout.childControlHeight = false;
            var streakFit = streakInner.gameObject.AddComponent<ContentSizeFitter>();
            streakFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            streakFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (GameArt.Flame != null)
            {
                var fi = new GameObject("Flame", typeof(RectTransform), typeof(Image));
                fi.transform.SetParent(streakInner, false);
                var fimg = fi.GetComponent<Image>();
                fimg.sprite = GameArt.SpriteFrom(GameArt.Flame);
                fimg.preserveAspect = true;
                fi.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
            }
            _streakLabel = UIFactory.Label(streakInner, "Streak", "", 28, _accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _streakLabel.rectTransform.sizeDelta = new Vector2(300, 40);

            _chapterLabel = UIFactory.Label(card, "Chapter", "Chapter 1", 34, _muted);
            _chapterLabel.rectTransform.sizeDelta = new Vector2(0, 46);
            var jtrackImg = UIFactory.RoundedPanel(card, "JTrack", UIFactory.Hex("#0c0e18"), 14);
            var jtrack = jtrackImg.rectTransform;
            jtrack.sizeDelta = new Vector2(0, 22);
            // A near-black track on a near-black card has almost no contrast
            // at 0% progress -- without this it reads as a random black smear
            // rather than a defined progress bar.
            var jtrackEdge = jtrackImg.gameObject.AddComponent<Outline>();
            jtrackEdge.effectColor = new Color(1f, 1f, 1f, 0.18f);
            jtrackEdge.effectDistance = new Vector2(1f, -1f);
            _journeyFill = UIFactory.RoundedPanel(jtrack, "JFill", _primary, 14, true);
            _journeyFill.rectTransform.anchorMin = new Vector2(0, 0);
            _journeyFill.rectTransform.anchorMax = new Vector2(0, 1);
            _journeyFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _journeyFill.rectTransform.offsetMin = Vector2.zero;
            _journeyFill.rectTransform.offsetMax = Vector2.zero;
            UIFactory.AddGloss(_journeyFill.transform, 0.3f, 1f, 0.08f);

            var play = UIFactory.Button(card, "Play", "PLAY", _primary, Color.white, 46);
            ((RectTransform)play.transform).sizeDelta = new Vector2(0, 130);
            play.onClick.AddListener(() => OnPlay?.Invoke());

            _dailyBtn = UIFactory.Button(card, "Daily", "Daily reward", _accent, Color.white, 32);
            ((RectTransform)_dailyBtn.transform).sizeDelta = new Vector2(0, 96);
            _dailyBtn.onClick.AddListener(() => OnDaily?.Invoke());

            UIFactory.Label(card, "MissHead", "DAILY MISSIONS", 26, _muted).rectTransform.sizeDelta = new Vector2(0, 40);
            _missionsList = UIFactory.Container(card, "Missions");
            _missionsList.sizeDelta = new Vector2(0, 220);
            var mv = _missionsList.gameObject.AddComponent<VerticalLayoutGroup>();
            mv.spacing = 10; mv.childControlHeight = false; mv.childForceExpandHeight = false;

            _collectionCount = UIFactory.Label(card, "ColHead", "Gallery 0/0", 26, _muted);
            _collectionCount.rectTransform.sizeDelta = new Vector2(0, 40);
            _collectionRow = UIFactory.Container(card, "Collection");
            _collectionRow.sizeDelta = new Vector2(0, 90);
            var cg = _collectionRow.gameObject.AddComponent<GridLayoutGroup>();
            cg.cellSize = new Vector2(64, 64); cg.spacing = new Vector2(8, 8);
            cg.constraint = GridLayoutGroup.Constraint.FixedRowCount; cg.constraintCount = 1;
        }

        void BuildLevelComplete()
        {
            // Light scrim: the freshly-revealed picture behind this card is
            // the level's payoff, so keep it visible rather than blacked out.
            _levelComplete = Overlay("LevelComplete", out var card, scrimAlpha: 0.45f);
            _levelComplete.gameObject.SetActive(false);
            if (GameArt.Chest != null)
            {
                var chestGo = new GameObject("ChestBadge", typeof(RectTransform), typeof(Image));
                chestGo.transform.SetParent(card, false);
                var cimg = chestGo.GetComponent<Image>();
                cimg.sprite = GameArt.SpriteFrom(GameArt.Chest);
                cimg.preserveAspect = true;
                _chestBadge = chestGo.GetComponent<RectTransform>();
                _chestBadge.sizeDelta = new Vector2(0, 180);
            }
            var celebrateMascot = GameArt.MascotHappy != null ? GameArt.MascotHappy : GameArt.Mascot;
            if (celebrateMascot != null) ImageFit(card, "Mascot", celebrateMascot, 220);

            _starRow = UIFactory.Container(card, "StarRow");
            _starRow.sizeDelta = new Vector2(0, 80);
            var starInner = UIFactory.Container(_starRow, "StarInner");
            starInner.anchorMin = starInner.anchorMax = new Vector2(0.5f, 0.5f);
            starInner.pivot = new Vector2(0.5f, 0.5f);
            var starLayout = starInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            starLayout.childAlignment = TextAnchor.MiddleCenter; starLayout.spacing = 14;
            starLayout.childControlWidth = false; starLayout.childControlHeight = false;
            var starFit = starInner.gameObject.AddComponent<ContentSizeFitter>();
            starFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            starFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var sg = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                sg.transform.SetParent(starInner, false);
                var simg = sg.GetComponent<Image>();
                if (GameArt.StarIcon != null) { simg.sprite = GameArt.SpriteFrom(GameArt.StarIcon); simg.preserveAspect = true; }
                else simg.color = UIFactory.Hex("#ffd76a");
                sg.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
                _starImages[i] = simg;
            }
            if (GameArt.StarIcon == null)
            {
                _starText = UIFactory.Label(_starRow, "Stars", "★ ★ ★", 64, UIFactory.Hex("#ffd76a"));
                UIFactory.Stretch(_starText.rectTransform);
            }

            _lcTitle = UIFactory.Label(card, "Title", "Board Cleared!", 60, _text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _lcTitle.rectTransform.sizeDelta = new Vector2(0, 90);
            _lcLevel = UIFactory.Label(card, "Lvl", "Level 1 complete", 34, _muted);
            _lcLevel.rectTransform.sizeDelta = new Vector2(0, 50);
            _lcPoints = UIFactory.Label(card, "Pts", "+0 pts", 40, _accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _lcPoints.rectTransform.sizeDelta = new Vector2(0, 60);
            var coinsRow = UIFactory.Container(card, "CoinsRow");
            coinsRow.sizeDelta = new Vector2(0, 56);
            var coinsInner = UIFactory.Container(coinsRow, "CoinsInner");
            coinsInner.anchorMin = coinsInner.anchorMax = new Vector2(0.5f, 0.5f);
            coinsInner.pivot = new Vector2(0.5f, 0.5f);
            var coinsLayout = coinsInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            coinsLayout.childAlignment = TextAnchor.MiddleCenter; coinsLayout.spacing = 8;
            coinsLayout.childControlWidth = false; coinsLayout.childControlHeight = false;
            var coinsFit = coinsInner.gameObject.AddComponent<ContentSizeFitter>();
            coinsFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            coinsFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (GameArt.Coin != null)
            {
                var ci = new GameObject("Coin", typeof(RectTransform), typeof(Image));
                ci.transform.SetParent(coinsInner, false);
                var cimg2 = ci.GetComponent<Image>();
                cimg2.sprite = GameArt.SpriteFrom(GameArt.Coin); cimg2.preserveAspect = true;
                ci.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
            }
            _lcCoins = UIFactory.Label(coinsInner, "Coins", "+0", 36, UIFactory.Hex("#ffd76a"));
            _lcCoins.rectTransform.sizeDelta = new Vector2(160, 50);
            _unlockNote = UIFactory.Label(card, "Unlock", "", 30, UIFactory.Hex("#7fe0a0"));
            _unlockNote.rectTransform.sizeDelta = new Vector2(0, 50);
            var next = UIFactory.Button(card, "Next", "NEXT LEVEL →", _primary, Color.white, 42);
            ((RectTransform)next.transform).sizeDelta = new Vector2(0, 130);
            next.onClick.AddListener(() => OnNextLevel?.Invoke());
        }

        void BuildGameOver()
        {
            _gameOver = Overlay("GameOver", out var card);
            _gameOver.gameObject.SetActive(false);
            if (GameArt.MascotSad != null) ImageFit(card, "SadMascot", GameArt.MascotSad, 260);
            else BuildBombBadge(card, 150);
            UIFactory.Label(card, "Title", "Out of lives!", 60, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.sizeDelta = new Vector2(0, 90);
            _nearMiss = UIFactory.Label(card, "Near", "", 34, _accent);
            _nearMiss.rectTransform.sizeDelta = new Vector2(0, 50);
            _goScore = UIFactory.Label(card, "Score", "Score: 0", 38, _muted);
            _goScore.rectTransform.sizeDelta = new Vector2(0, 60);
            var cont = UIFactory.Button(card, "Continue", "Continue — clear the bombs", _accent, Color.white, 30);
            ((RectTransform)cont.transform).sizeDelta = new Vector2(0, 120);
            cont.onClick.AddListener(() => OnContinueAd?.Invoke());
            if (GameArt.AdTag != null)
            {
                var ai = new GameObject("AdTag", typeof(RectTransform), typeof(Image));
                ai.transform.SetParent(cont.transform, false);
                var aimg = ai.GetComponent<Image>();
                aimg.sprite = GameArt.SpriteFrom(GameArt.AdTag);
                aimg.preserveAspect = true;
                aimg.raycastTarget = false;
                var art = ai.GetComponent<RectTransform>();
                art.anchorMin = art.anchorMax = new Vector2(1f, 0.5f);
                art.pivot = new Vector2(1f, 0.5f);
                art.anchoredPosition = new Vector2(-18, 0);
                art.sizeDelta = new Vector2(52, 52);
            }
            var retry = UIFactory.Button(card, "Retry", "Retry level", _cardBg, _text, 34);
            ((RectTransform)retry.transform).sizeDelta = new Vector2(0, 100);
            retry.onClick.AddListener(() => OnRetry?.Invoke());
        }

        void BuildSettings()
        {
            _settings = Overlay("Settings", out var card);
            _settings.gameObject.SetActive(false);
            UIFactory.Label(card, "Title", "Settings", 56, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.sizeDelta = new Vector2(0, 90);
            var snd = UIFactory.Button(card, "Sound", "Sound: On", _cardBg, _text, 34);
            ((RectTransform)snd.transform).sizeDelta = new Vector2(0, 100);
            snd.onClick.AddListener(() => { OnToggleSound?.Invoke(); snd.GetComponentInChildren<Text>().text = "Sound: " + (SaveSystem.SoundOn ? "On" : "Off"); });
            if (GameArt.SoundIcon != null)
            {
                var si = new GameObject("SoundIcon", typeof(RectTransform), typeof(Image));
                si.transform.SetParent(snd.transform, false);
                var simg = si.GetComponent<Image>();
                simg.sprite = GameArt.SpriteFrom(GameArt.SoundIcon);
                simg.preserveAspect = true;
                simg.raycastTarget = false;
                var srt = si.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0f, 0.5f);
                srt.pivot = new Vector2(0f, 0.5f);
                srt.anchoredPosition = new Vector2(20, 0);
                srt.sizeDelta = new Vector2(56, 56);
            }
            UIFactory.Label(card, "Ver", "Reveal 3.0 · numbers show bombs nearby · clear 70%", 24, _muted)
                .rectTransform.sizeDelta = new Vector2(0, 60);
            var done = UIFactory.Button(card, "Done", "Done", _primary, Color.white, 38);
            ((RectTransform)done.transform).sizeDelta = new Vector2(0, 110);
            done.onClick.AddListener(() => OnCloseSettings?.Invoke());

            var home = UIFactory.Button(card, "Home", "Main menu", _cardBg, _text, 32);
            ((RectTransform)home.transform).sizeDelta = new Vector2(0, 96);
            home.onClick.AddListener(() => OnHome?.Invoke());
            var reset = UIFactory.Button(card, "Reset", "Reset all progress", _cardBg, UIFactory.Hex("#ff5f7e"), 30);
            ((RectTransform)reset.transform).sizeDelta = new Vector2(0, 90);
            reset.onClick.AddListener(() => OnReset?.Invoke());
        }

        // ---------------- update methods ----------------
        public void SetHud(int coins, int level, int score, int best)
        {
            _coins.text = coins.ToString();
            if (_menuCoins != null) _menuCoins.text = coins.ToString();
            _level.text = level.ToString();
            _score.text = score.ToString();
            _best.text = best.ToString();
        }

        public void SetProgress(float frac, int remaining)
        {
            frac = Mathf.Clamp01(frac);
            _progressFill.gameObject.SetActive(frac > 0.02f);
            _progressFill.rectTransform.anchorMax = new Vector2(frac, 1);
            _progressPct.text = Mathf.RoundToInt(frac * 100) + "%";
            _progressFill.color = frac >= GameConfig.GlowAt ? UIFactory.Hex("#7fe0a0") : _accent;
        }

        public void SetHearts(int hearts)
        {
            foreach (Transform t in _heartsRow) Destroy(t.gameObject);
            for (int i = 0; i < GameConfig.MaxHearts; i++)
            {
                bool full = i < hearts;
                if (GameArt.HeartIcon != null)
                {
                    var go = new GameObject("H", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(_heartsRow, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = GameArt.SpriteFrom(GameArt.HeartIcon);
                    img.preserveAspect = true;
                    img.color = full ? Color.white : new Color(1f, 1f, 1f, 0.25f);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(44, 44);
                }
                else
                {
                    var h = UIFactory.Label(_heartsRow, "H", full ? "♥" : "♡", 40,
                        full ? UIFactory.Hex("#ff5f7e") : _muted);
                    h.rectTransform.sizeDelta = new Vector2(44, 44);
                }
            }
        }

        public void SetBombInfo(int bombs)
        {
            _bombInfo.text = bombs <= 0 ? "no bombs left — scratch freely!"
                : bombs == 1 ? "1 bomb hidden"
                : $"{bombs} bombs hidden";
        }

        public void SetHintButton(int coins)
        {
            bool usesAd = coins < GameConfig.HintCost;
            var t = _hintBtn.GetComponentInChildren<Text>();
            t.text = usesAd
                ? "Reveal a safe tile"
                : $"Reveal a safe tile · {GameConfig.HintCost} coins";
            if (_hintAdTag != null) _hintAdTag.SetActive(usesAd);
        }

        public void ShowInGame(bool show)
        {
            _hintBtn.gameObject.SetActive(show);
        }

        public void ShowMenu(bool show) => _menu.gameObject.SetActive(show);
        public void ShowSettings(bool show) => _settings.gameObject.SetActive(show);

        public void ShowLevelComplete(int level, int points, int coins, string unlock, int stars, bool isChest)
        {
            if (_starImages != null)
            {
                for (int i = 0; i < _starImages.Length; i++)
                    _starImages[i].color = i < stars ? Color.white : new Color(1f, 1f, 1f, 0.22f);
            }
            else if (_starText != null)
            {
                string on = "<color=#ffd76a>★</color>";
                string off = "<color=#3a3f55>★</color>";
                _starText.text = string.Join(" ",
                    new[] { stars >= 1 ? on : off, stars >= 2 ? on : off, stars >= 3 ? on : off });
            }
            if (_chestBadge != null) _chestBadge.gameObject.SetActive(isChest);
            _lcLevel.text = $"Level {level} complete";
            _lcPoints.text = $"+{points} pts";
            _lcCoins.text = $"+{coins}";
            _unlockNote.text = unlock ?? "";
            _unlockNote.gameObject.SetActive(!string.IsNullOrEmpty(unlock));
            _levelComplete.gameObject.SetActive(true);
            Confetti.Burst(_levelComplete);
        }
        public void HideLevelComplete() => _levelComplete.gameObject.SetActive(false);

        public void ShowGameOver(int score, string nearMiss)
        {
            _goScore.text = $"Score: {score}";
            _nearMiss.text = nearMiss ?? "";
            _nearMiss.gameObject.SetActive(!string.IsNullOrEmpty(nearMiss));
            _gameOver.gameObject.SetActive(true);
        }
        public void HideGameOver() => _gameOver.gameObject.SetActive(false);

        public void SetMenuMeta(int streak, int level, HashSet<int> collection)
        {
            _streakLabel.text = streak > 0 ? $"{streak}-DAY STREAK" : "";
            _streakRow.gameObject.SetActive(streak > 0);
            _chapterLabel.text = $"Chapter {GameConfig.ChapterOf(level)} · Level {level}";
            int into = (level - 1) % GameConfig.ChestEvery;
            float jf = (float)into / GameConfig.ChestEvery;
            _journeyFill.gameObject.SetActive(jf > 0.02f);
            _journeyFill.rectTransform.anchorMax = new Vector2(jf, 1);
            _collectionCount.text = $"Gallery {collection.Count}/{Scenes.Count}";

            foreach (Transform t in _collectionRow) Destroy(t.gameObject);
            for (int i = 0; i < Scenes.Count; i++)
            {
                bool got = collection.Contains(i);
                var cell = UIFactory.RoundedPanel(_collectionRow, "g", got ? Scenes.All[i].BgTop : UIFactory.Hex("#20233a"), 16, got);
                var pic = got ? GameArt.Picture(Scenes.All[i].Motif) : null;
                if (pic != null) { cell.sprite = GameArt.SpriteFrom(pic); cell.type = Image.Type.Simple; cell.preserveAspect = true; cell.color = Color.white; }
                else if (!got && GameArt.Locked != null)
                {
                    cell.sprite = GameArt.SpriteFrom(GameArt.Locked);
                    cell.type = Image.Type.Simple;
                    cell.preserveAspect = true;
                    cell.color = Color.white;
                }
            }
        }

        public void SetDailyButton(bool available, int amount)
        {
            _dailyBtn.gameObject.SetActive(available);
            if (available) _dailyBtn.GetComponentInChildren<Text>().text = $"Daily reward · +{amount} coins";
        }

        public void SetMissions(List<Mission> missions)
        {
            foreach (Transform t in _missionsList) Destroy(t.gameObject);
            foreach (var m in missions)
            {
                var row = UIFactory.RoundedPanel(_missionsList, "m", UIFactory.Hex("#0c0e18"), 20).rectTransform;
                row.sizeDelta = new Vector2(0, 64);

                // Distinct icon per mission type -- Chest and Coin alone are
                // both warm-toned circular badges that blur together at
                // small size, and FindBonus/EarnCoins previously shared the
                // same Coin icon with nothing to tell them apart.
                Texture2D icon = m.Type == MissionType.ClearLevels ? GameArt.Chest
                    : m.Type == MissionType.FindBonus ? GameArt.StarIcon
                    : GameArt.Coin;
                float labelStart = 24;
                if (icon != null)
                {
                    var ig = new GameObject("icon", typeof(RectTransform), typeof(Image));
                    ig.transform.SetParent(row, false);
                    var iimg = ig.GetComponent<Image>();
                    iimg.sprite = GameArt.SpriteFrom(icon);
                    iimg.preserveAspect = true;
                    var irt = ig.GetComponent<RectTransform>();
                    irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
                    irt.pivot = new Vector2(0, 0.5f);
                    irt.anchoredPosition = new Vector2(14, 0);
                    irt.sizeDelta = new Vector2(46, 46);
                    labelStart = 70;
                }

                var label = UIFactory.Label(row, "t", $"{m.Text}", 26, m.Complete ? UIFactory.Hex("#7fe0a0") : _text, TextAnchor.MiddleLeft);
                label.rectTransform.anchorMin = new Vector2(0, 0); label.rectTransform.anchorMax = new Vector2(0.68f, 1);
                label.rectTransform.offsetMin = new Vector2(labelStart, 0); label.rectTransform.offsetMax = Vector2.zero;
                var prog = UIFactory.Label(row, "p", $"{m.Progress}/{m.Goal}", 26, _muted, TextAnchor.MiddleRight);
                prog.rectTransform.anchorMin = new Vector2(0.68f, 0); prog.rectTransform.anchorMax = new Vector2(1, 1);
                prog.rectTransform.offsetMin = Vector2.zero; prog.rectTransform.offsetMax = new Vector2(-24, 0);
            }
        }
    }
}
