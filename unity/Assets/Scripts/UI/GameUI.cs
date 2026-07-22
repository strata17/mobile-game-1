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
        Text _chapterLabel, _streakLabel, _collectionCount, _menuCoins;
        RectTransform _streakRow;
        Image _journeyFill;
        Button _dailyBtn;
        RectTransform _missionsList, _collectionRow;
        Button _hintBtn;

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
            var hudBg = UIFactory.RoundedPanel(_chrome, "HudBg", new Color(_cardBg.r, _cardBg.g, _cardBg.b, 0.55f), 36).rectTransform;
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
            var track = UIFactory.RoundedPanel(prow, "Track", UIFactory.Hex("#0c0e18"), 18).rectTransform;
            UIFactory.Stretch(track);
            _progressFill = UIFactory.RoundedPanel(track, "Fill", _accent, 18, true);
            _progressFill.rectTransform.anchorMin = new Vector2(0, 0);
            _progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            _progressFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _progressFill.rectTransform.sizeDelta = new Vector2(0, 0);
            _progressPct = UIFactory.Label(prow, "Pct", "0%", 26, _text);
            UIFactory.Stretch(_progressPct.rectTransform);

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
        }

        // ---------------- overlays ----------------
        RectTransform Overlay(string name, out RectTransform card, bool opaque = false)
        {
            RectTransform ov;
            if (opaque)
            {
                // Full opaque gradient — a real screen, nothing bleeds behind it.
                var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(_root, false);
                var ri = go.GetComponent<RawImage>();
                var bgArt = GameArt.Background;
                ri.texture = bgArt != null ? bgArt : Art.Gradient(UIFactory.Hex("#4a3aa8"), UIFactory.Hex("#140f30"));
                ov = go.GetComponent<RectTransform>();
            }
            else
            {
                ov = UIFactory.Panel(_root, name, new Color(0.02f, 0.03f, 0.06f, 0.62f));
            }
            UIFactory.Stretch(ov);

            card = UIFactory.RoundedPanel(ov, "Card", _cardBg, 44).rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(920, 0);
            var cardShadow = card.gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            cardShadow.effectDistance = new Vector2(0, -10);

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

            // Card: a slim frosted panel in the lower two-thirds; the hero art
            // (mascot + logo + coin) sits on the background above it.
            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(_cardBg.r, _cardBg.g, _cardBg.b, 0.9f);
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
            var jtrack = UIFactory.RoundedPanel(card, "JTrack", UIFactory.Hex("#0c0e18"), 14).rectTransform;
            jtrack.sizeDelta = new Vector2(0, 22);
            _journeyFill = UIFactory.RoundedPanel(jtrack, "JFill", _primary, 14, true);
            _journeyFill.rectTransform.anchorMin = new Vector2(0, 0);
            _journeyFill.rectTransform.anchorMax = new Vector2(0, 1);
            _journeyFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _journeyFill.rectTransform.offsetMin = Vector2.zero;
            _journeyFill.rectTransform.offsetMax = Vector2.zero;

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
            _levelComplete = Overlay("LevelComplete", out var card);
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
            if (GameArt.Mascot != null) ImageFit(card, "Mascot", GameArt.Mascot, 220);

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
            UIFactory.Label(card, "Boom", "!", 110, UIFactory.Hex("#ff5f7e"), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.sizeDelta = new Vector2(0, 120);
            UIFactory.Label(card, "Title", "Out of lives!", 60, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.sizeDelta = new Vector2(0, 90);
            _nearMiss = UIFactory.Label(card, "Near", "", 34, _accent);
            _nearMiss.rectTransform.sizeDelta = new Vector2(0, 50);
            _goScore = UIFactory.Label(card, "Score", "Score: 0", 38, _muted);
            _goScore.rectTransform.sizeDelta = new Vector2(0, 60);
            var cont = UIFactory.Button(card, "Continue", "Continue — clear the bombs (AD)", _accent, Color.white, 30);
            ((RectTransform)cont.transform).sizeDelta = new Vector2(0, 120);
            cont.onClick.AddListener(() => OnContinueAd?.Invoke());
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

        public void SetHintButton(int coins)
        {
            var t = _hintBtn.GetComponentInChildren<Text>();
            t.text = coins >= GameConfig.HintCost
                ? $"Reveal a safe tile · {GameConfig.HintCost} coins"
                : "Reveal a safe tile (AD)";
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
                var label = UIFactory.Label(row, "t", $"{m.Text}", 28, m.Complete ? UIFactory.Hex("#7fe0a0") : _text, TextAnchor.MiddleLeft);
                label.rectTransform.anchorMin = new Vector2(0, 0); label.rectTransform.anchorMax = new Vector2(0.7f, 1);
                label.rectTransform.offsetMin = new Vector2(24, 0); label.rectTransform.offsetMax = Vector2.zero;
                var prog = UIFactory.Label(row, "p", $"{m.Progress}/{m.Goal}", 26, _muted, TextAnchor.MiddleRight);
                prog.rectTransform.anchorMin = new Vector2(0.7f, 0); prog.rectTransform.anchorMax = new Vector2(1, 1);
                prog.rectTransform.offsetMin = Vector2.zero; prog.rectTransform.offsetMax = new Vector2(-24, 0);
            }
        }
    }
}
