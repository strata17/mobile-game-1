using System.Collections.Generic;
using Reveal.Core;
using UnityEngine;

namespace Reveal.Game
{
    public enum RevealResult { Nothing, Safe, Bonus, Bomb, Win }

    /// <summary>
    /// Pure game-logic model of one level's grid. No rendering. Owns bombs,
    /// bonus tiles, the endowed head-start, and win detection at 70% of safe
    /// tiles. The view calls Reveal() and reacts to the result.
    /// </summary>
    public class Board
    {
        public int Size { get; private set; }
        public int Level { get; private set; }

        public bool[,] Revealed;
        public bool[,] Bomb;
        public bool[,] Bonus;
        public int[,] Adj;      // number of bombs in the 8 neighbours (Minesweeper clue)

        bool _firstMove = true; // the player's first tap is always safe

        public int NonBombTotal { get; private set; }
        public int RevealedSafe { get; private set; }
        public int BonusFound { get; private set; }

        public int Threshold => Mathf.CeilToInt(NonBombTotal * GameConfig.WinRatio);
        public bool Won => RevealedSafe >= Threshold;
        public int RemainingToWin => Mathf.Max(0, Threshold - RevealedSafe);
        public float Progress => Threshold == 0 ? 0f : Mathf.Clamp01((float)RevealedSafe / Threshold);

        public Board(int level)
        {
            Level = level;
            Build();
        }

        void Build()
        {
            Size = GameConfig.GridSizeForLevel(Level);
            Revealed = new bool[Size, Size];
            Bomb = new bool[Size, Size];
            Bonus = new bool[Size, Size];

            int cells = Size * Size;
            int target = GameConfig.BombCountForLevel(Level, cells);

            var rng = new System.Random();
            int placed = 0;
            while (placed < target)
            {
                int r = rng.Next(Size), c = rng.Next(Size);
                if (!Bomb[r, c]) { Bomb[r, c] = true; placed++; }
            }

            NonBombTotal = cells - placed;

            // Variable-ratio reward (Skinner): 1-3 hidden bonus tiles on safe cells.
            int bonusCount = 1 + rng.Next(3);
            int placedBonus = 0, guard = 0;
            while (placedBonus < bonusCount && guard++ < cells * 4)
            {
                int r = rng.Next(Size), c = rng.Next(Size);
                if (!Bomb[r, c] && !Bonus[r, c]) { Bonus[r, c] = true; placedBonus++; }
            }

            // Endowed progress effect: pre-reveal a few safe tiles so the player
            // starts the level already making visible progress toward the goal.
            var safe = new List<(int, int)>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (!Bomb[r, c] && !Bonus[r, c]) safe.Add((r, c));
            Shuffle(safe, rng);
            for (int i = 0; i < GameConfig.EndowTiles && i < safe.Count; i++)
            {
                var (r, c) = safe[i];
                Revealed[r, c] = true;
                RevealedSafe++;
            }

            ComputeAdj();
        }

        /// <summary>Recompute the adjacent-bomb count for every cell.</summary>
        void ComputeAdj()
        {
            Adj = new int[Size, Size];
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    if (Bomb[r, c]) continue;
                    int n = 0;
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue;
                            int nr = r + dr, nc = c + dc;
                            if (nr >= 0 && nc >= 0 && nr < Size && nc < Size && Bomb[nr, nc]) n++;
                        }
                    Adj[r, c] = n;
                }
        }

        public int Adjacent(int r, int c) => Adj[r, c];

        public RevealResult Reveal(int r, int c)
        {
            if (r < 0 || c < 0 || r >= Size || c >= Size) return RevealResult.Nothing;
            if (Revealed[r, c]) return RevealResult.Nothing;

            // The first tap of a level never detonates — relocate the bomb.
            if (_firstMove)
            {
                _firstMove = false;
                if (Bomb[r, c]) RelocateBomb(r, c);
            }

            if (Bomb[r, c])
            {
                Revealed[r, c] = true;
                return RevealResult.Bomb;
            }

            Revealed[r, c] = true;
            RevealedSafe++;

            if (Bonus[r, c]) BonusFound++;

            if (Won) return RevealResult.Win;
            return Bonus[r, c] ? RevealResult.Bonus : RevealResult.Safe;
        }

        /// <summary>Find a hidden safe tile for the hint / continue features.</summary>
        public bool TryFindSafeHidden(out int rr, out int cc)
        {
            var opts = new List<(int, int)>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (!Revealed[r, c] && !Bomb[r, c]) opts.Add((r, c));
            if (opts.Count == 0) { rr = cc = -1; return false; }
            var pick = opts[Random.Range(0, opts.Count)];
            rr = pick.Item1; cc = pick.Item2;
            return true;
        }

        /// <summary>Move the bomb at (r,c) to a random hidden safe cell.</summary>
        void RelocateBomb(int r, int c)
        {
            var opts = new List<(int, int)>();
            for (int rr = 0; rr < Size; rr++)
                for (int cc = 0; cc < Size; cc++)
                    if (!Bomb[rr, cc] && !Revealed[rr, cc] && !(rr == r && cc == c))
                        opts.Add((rr, cc));
            if (opts.Count == 0) return;
            var pick = opts[new System.Random().Next(opts.Count)];
            Bomb[r, c] = false;
            Bomb[pick.Item1, pick.Item2] = true;
            ComputeAdj();
        }

        /// <summary>Continue reward: clear every bomb so the run can resume.</summary>
        public void DefuseAllBombs()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    Bomb[r, c] = false;
            ComputeAdj();
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
