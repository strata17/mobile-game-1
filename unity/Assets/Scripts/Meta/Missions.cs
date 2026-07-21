using System.Collections.Generic;
using System.Linq;
using Reveal.Core;

namespace Reveal.Meta
{
    public enum MissionType { ClearLevels, FindBonus, EarnCoins }

    public class Mission
    {
        public string Id;
        public MissionType Type;
        public int Goal;
        public int Progress;
        public int Reward;
        public string TextTemplate;
        public bool Claimed;

        public bool Complete => Progress >= Goal;
        public string Text => TextTemplate.Replace("{goal}", Goal.ToString());
    }

    /// <summary>
    /// Daily missions: three rolled per UTC day, persisted with progress.
    /// A steady, low-friction reason to come back (Zeigarnik / completionism).
    /// </summary>
    public static class Missions
    {
        static readonly (string id, MissionType type, int goal, int reward, string text)[] Pool =
        {
            ("clear3", MissionType.ClearLevels, 3, 40, "Clear {goal} levels"),
            ("clear6", MissionType.ClearLevels, 6, 80, "Clear {goal} levels"),
            ("bonus4", MissionType.FindBonus, 4, 50, "Find {goal} bonus tiles"),
            ("bonus8", MissionType.FindBonus, 8, 90, "Find {goal} bonus tiles"),
            ("coins150", MissionType.EarnCoins, 150, 60, "Earn {goal} coins"),
            ("coins300", MissionType.EarnCoins, 300, 110, "Earn {goal} coins"),
        };

        public static List<Mission> Active { get; private set; } = new List<Mission>();

        public static void Load()
        {
            if (SaveSystem.MissionsDay != SaveSystem.Today)
            {
                Roll();
                return;
            }
            Active = Deserialize(SaveSystem.MissionsBlob);
            if (Active.Count == 0) Roll();
        }

        static void Roll()
        {
            var rng = new System.Random();
            Active = Pool.OrderBy(_ => rng.Next()).Take(3).Select(p => new Mission
            {
                Id = p.id, Type = p.type, Goal = p.goal, Reward = p.reward,
                TextTemplate = p.text, Progress = 0, Claimed = false
            }).ToList();
            SaveSystem.MissionsDay = SaveSystem.Today;
            Save();
        }

        public static void Progress(MissionType type, int amount)
        {
            foreach (var m in Active)
                if (m.Type == type && !m.Complete)
                    m.Progress = System.Math.Min(m.Goal, m.Progress + amount);
            Save();
        }

        /// <summary>Claim any newly-completed missions; returns total coin reward.</summary>
        public static int ClaimCompleted()
        {
            int total = 0;
            foreach (var m in Active)
                if (m.Complete && !m.Claimed) { m.Claimed = true; total += m.Reward; }
            if (total > 0) Save();
            return total;
        }

        static void Save() => SaveSystem.MissionsBlob = Serialize(Active);

        static string Serialize(List<Mission> ms) =>
            string.Join(";", ms.Select(m => $"{m.Id}|{m.Progress}|{(m.Claimed ? 1 : 0)}"));

        static List<Mission> Deserialize(string blob)
        {
            var list = new List<Mission>();
            if (string.IsNullOrEmpty(blob)) return list;
            foreach (var part in blob.Split(';'))
            {
                var f = part.Split('|');
                if (f.Length < 2) continue;
                var def = Pool.FirstOrDefault(p => p.id == f[0]);
                if (def.id == null) continue;
                int.TryParse(f[1], out int prog);
                bool claimed = f.Length > 2 && f[2] == "1";
                list.Add(new Mission
                {
                    Id = def.id, Type = def.type, Goal = def.goal, Reward = def.reward,
                    TextTemplate = def.text, Progress = prog, Claimed = claimed
                });
            }
            return list;
        }
    }
}
