using System.Collections.Generic;
using Reveal.Ads;
using Reveal.Audio;
using Reveal.Core;
using Reveal.Meta;
using Reveal.UI;
using UnityEngine;

namespace Reveal.Game
{
    /// <summary>
    /// The game's brain: owns the run state, the current Board, and drives the
    /// UI. Faithfully ports the web prototype's loop — endowed progress, hearts,
    /// visible bombs, bonus tiles, 70% win threshold, chapter chests, near-miss,
    /// daily reward, missions and the picture collection.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        GameUI _ui;
        BoardView _view;
        Board _board;

        int _level, _score, _best, _coins, _hearts;
        bool _playing;
        bool _usedContinueThisRun;
        float _startTime;
        Texture2D _pic;

        public void Init(GameUI ui, BoardView view)
        {
            _ui = ui;
            _view = view;
            _view.OnReveal = OnScratch;

            WireUi();

            _level = SaveSystem.Level;
            _coins = SaveSystem.Coins;
            _best = SaveSystem.Best;

            Missions.Load();
            RefreshMenu();

            _ui.ShowInGame(false);
            _ui.ShowMenu(true);
            _ui.SetHud(_coins, _level, 0, _best);
        }

        void WireUi()
        {
            _ui.OnPlay = StartLevel;
            _ui.OnNextLevel = () => { _ui.HideLevelComplete(); StartLevel(); };
            _ui.OnRetry = () => { _ui.HideGameOver(); StartLevel(); };
            _ui.OnDaily = ClaimDaily;
            _ui.OnHint = UseHint;
            _ui.OnContinueAd = ContinueWithAd;
            _ui.OnSettings = () => _ui.ShowSettings(true);
            _ui.OnCloseSettings = () => _ui.ShowSettings(false);
            _ui.OnReset = ResetProgress;
            _ui.OnToggleSound = () => SaveSystem.SoundOn = !SaveSystem.SoundOn;
            _ui.OnHome = () =>
            {
                _playing = false;
                _ui.ShowSettings(false);
                _ui.HideLevelComplete();
                _ui.HideGameOver();
                _ui.ShowInGame(false);
                RefreshMenu();
            };
        }

        // ---------------- flow ----------------
        void StartLevel()
        {
            _ui.ShowMenu(false);
            _ui.HideLevelComplete();
            _ui.HideGameOver();

            _board = new Board(_level);
            _hearts = GameConfig.MaxHearts;
            _score = 0;
            _usedContinueThisRun = false;
            _playing = true;
            _startTime = Time.time;

            var scene = Scenes.ForLevel(_level);
            var real = Reveal.UI.GameArt.Picture(scene.Motif);
            _pic = real != null ? real : MotifPainter.Paint(scene);
            float sizePx = _ui.BoardHost.rect.width;
            if (sizePx <= 1) sizePx = 980;
            _view.Load(_board, _pic, sizePx);

            _ui.ShowInGame(true);
            _ui.SetHud(_coins, _level, _score, _best);
            _ui.SetHearts(_hearts);
            _ui.SetHintButton(_coins);
            _ui.ShowTutorial(!SaveSystem.TutorialDone);
            UpdateProgress();
        }

        void OnScratch(int r, int c)
        {
            if (!_playing) return;
            if (!SaveSystem.TutorialDone)
            {
                SaveSystem.TutorialDone = true;
                _ui.ShowTutorial(false);
            }
            var result = _board.Reveal(r, c);
            switch (result)
            {
                case RevealResult.Nothing:
                    return;
                case RevealResult.Safe:
                    _view.RevealTile(r, c);
                    _view.ShowClue(r, c, _board.Adjacent(r, c));
                    _score += 10;
                    Sfx.Instance.Scratch();
                    break;
                case RevealResult.Bonus:
                    _view.RevealTile(r, c);
                    _view.ShowClue(r, c, _board.Adjacent(r, c));
                    AddCoins(GameConfig.BonusCoin);
                    Missions.Progress(MissionType.FindBonus, 1);
                    Missions.Progress(MissionType.EarnCoins, GameConfig.BonusCoin);
                    Sfx.Instance.Bonus();
                    break;
                case RevealResult.Bomb:
                    _view.RevealTile(r, c);
                    _hearts--;
                    _ui.SetHearts(_hearts);
                    Sfx.Instance.Bomb();
                    Haptics.Buzz();
                    if (_hearts <= 0) { GameOver(); return; }
                    break;
                case RevealResult.Win:
                    _view.RevealTile(r, c);
                    _view.ShowClue(r, c, _board.Adjacent(r, c));
                    Sfx.Instance.Win();
                    Haptics.Buzz();
                    LevelComplete();
                    return;
            }
            _ui.SetHud(_coins, _level, _score, _best);
            UpdateProgress();
        }

        void UpdateProgress()
        {
            _ui.SetProgress(_board.Progress, _board.RemainingToWin);
        }

        void LevelComplete()
        {
            _playing = false;

            float elapsed = Time.time - _startTime;
            int stars = elapsed <= GameConfig.StarTimes[0] ? 3 : elapsed <= GameConfig.StarTimes[1] ? 2 : 1;
            int points = 50 * _level + _score;
            int coins = GameConfig.LevelCoin + (_hearts == GameConfig.MaxHearts ? 10 : 0);

            _score += points;
            if (_score > _best) { _best = _score; SaveSystem.Best = _best; }

            // Collection: record the revealed picture (note first-time unlocks).
            int motifIndex = (_level - 1) % Scenes.Count;
            var col = SaveSystem.Collection;
            bool newPicture = !col.Contains(motifIndex);
            col.Add(motifIndex);
            SaveSystem.Collection = col;

            // Missions
            Missions.Progress(MissionType.ClearLevels, 1);
            int missionReward = Missions.ClaimCompleted();
            if (missionReward > 0) coins += missionReward;

            // Chapter chest every N levels
            string unlock = null;
            bool isChest = _level % GameConfig.ChestEvery == 0;
            if (isChest)
            {
                int chestCoins = 50 * GameConfig.ChapterOf(_level);
                coins += chestCoins;
                unlock = $"Chapter chest! +{chestCoins} coins";
            }
            else if (newPicture)
            {
                unlock = "New picture added to your gallery!";
            }

            AddCoins(coins);
            Missions.Progress(MissionType.EarnCoins, coins);

            _level++;
            SaveSystem.Level = _level;

            _ui.ShowInGame(false);
            _ui.SetHud(_coins, _level, _score, _best);
            _ui.ShowLevelComplete(_level - 1, points, coins, unlock, stars, isChest);
            RefreshMenu();
        }

        void GameOver()
        {
            _playing = false;
            _ui.ShowInGame(false);
            _view.RevealBombs();   // show where the bombs were — feels fair, not random

            // Near-miss framing (loss aversion): how close were they?
            int pct = Mathf.Min(99, Mathf.RoundToInt(_board.Progress * 100));
            string near = pct >= 60 ? $"So close — {pct}% revealed!" : null;

            AdManager.Instance.MaybeShowInterstitialOnLoss(_level, () =>
            {
                _ui.ShowGameOver(_score, near);
            });
        }

        // ---------------- rewards / ads ----------------
        void ContinueWithAd()
        {
            if (_usedContinueThisRun) return;
            AdManager.Instance.ShowRewarded(ok =>
            {
                if (!ok) return;
                _usedContinueThisRun = true;
                _board.DefuseAllBombs();
                _view.Load(_board, _pic, _ui.BoardHost.rect.width);
                _hearts = GameConfig.MaxHearts;
                _playing = true;
                _ui.HideGameOver();
                _ui.ShowInGame(true);
                _ui.SetHearts(_hearts);
                UpdateProgress();
            });
        }

        void UseHint()
        {
            if (!_playing) return;
            if (_coins >= GameConfig.HintCost)
            {
                AddCoins(-GameConfig.HintCost);
                DoHintReveal();
            }
            else
            {
                AdManager.Instance.ShowRewarded(ok => { if (ok) DoHintReveal(); });
            }
            _ui.SetHintButton(_coins);
        }

        void DoHintReveal()
        {
            if (_board.TryFindSafeHidden(out int r, out int c))
            {
                var res = _board.Reveal(r, c);
                _view.RevealTile(r, c);
                _view.ShowClue(r, c, _board.Adjacent(r, c));
                Sfx.Instance.Coin();
                if (res == RevealResult.Win) { LevelComplete(); return; }
                _ui.SetHud(_coins, _level, _score, _best);
                UpdateProgress();
            }
        }

        // ---------------- meta ----------------
        void ClaimDaily()
        {
            if (SaveSystem.LastDailyDay == SaveSystem.Today) return;

            // Streak: consecutive days extend it, a gap resets to 1.
            string yesterday = System.DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            SaveSystem.Streak = SaveSystem.LastDailyDay == yesterday ? SaveSystem.Streak + 1 : 1;
            SaveSystem.LastDailyDay = SaveSystem.Today;

            int amount = GameConfig.DailyAmount(SaveSystem.Streak);
            AddCoins(amount);
            Sfx.Instance.Coin();
            RefreshMenu();
        }

        void ResetProgress()
        {
            SaveSystem.ResetAll();
            _level = 1; _coins = 0; _best = 0; _score = 0;
            Missions.Load();
            _ui.ShowSettings(false);
            _ui.SetHud(_coins, _level, _score, _best);
            RefreshMenu();
        }

        void RefreshMenu()
        {
            _ui.SetMenuMeta(SaveSystem.Streak, _level, SaveSystem.Collection);
            bool dailyAvailable = SaveSystem.LastDailyDay != SaveSystem.Today;
            _ui.SetDailyButton(dailyAvailable, GameConfig.DailyAmount(SaveSystem.Streak + 1));
            _ui.SetMissions(Missions.Active);
            _ui.ShowMenu(true);
        }

        void AddCoins(int delta)
        {
            _coins = Mathf.Max(0, _coins + delta);
            SaveSystem.Coins = _coins;
            _ui.SetHud(_coins, _level, _score, _best);
        }
    }
}
