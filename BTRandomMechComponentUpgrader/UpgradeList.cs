using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class UpgradeList : IComparable<UpgradeList>
    {
        public List<UpgradeEntry[]> Upgrades = new List<UpgradeEntry[]>();
        public string[] Factions = new string[] { };
        public float UpgradePerComponentChance = 0.5f;
        public string[] CanRemove = new string[] { };
        public float RemoveMaxFactor = 0.5f;
        public List<UpgradeEntry[]> Additions = new List<UpgradeEntry[]>();
        public bool AllowDowngrade = false;
        public string[] LoadUpgrades = new string[] { };
        public string[] LoadAdditions = new string[] { };
        public string Name;
        public int Sort = 0;

        private static void CalculateLimit(UpgradeEntry[] ut, DateTime d)
        {
            float cw = ut.Sum((u) => CheckUpgradeCond(u, d) ? u.Weight : 0);
            float last = 0;
            foreach (UpgradeEntry u in ut)
            {
                if (CheckUpgradeCond(u, d))
                    last += u.Weight / cw;
                u.RandomLimit = last;
            }
        }

        public bool DoesApplyToFaction(string fac)
        {
            return Factions.Contains("default_all") || Factions.Contains(fac);
        }

        public UpgradeEntry[] GetUpgradeArrayAndOffset(string comp, out int min)
        {
            foreach (UpgradeEntry[] list in Upgrades)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    UpgradeEntry u = list[i];
                    if (u.ID.Equals(comp) && !u.ListLink)
                    {
                        min = (u.AllowDowngrade || this.AllowDowngrade) ? -1 : i;
                        return list;
                    }
                }
            }
            min = -1;
            return null;
        }

        private static bool CheckUpgradeCond(UpgradeEntry u, DateTime d)
        {
            if (u.MinDate > d)
                return false;
            return true;
        }

        public UpgradeEntry RollEntryFromMatchingSubList(string baseid, NetworkRandom nr, DateTime date, ref string log)
        {
            UpgradeEntry[] sublist = GetUpgradeArrayAndOffset(baseid, out int min);
            UpgradeEntry r = null;
            if (sublist != null)
            {
                r = RollEntryFromSubList(sublist, nr, min, date, ref log);
            }
            else
                log += " no sublist found";
            return r;
        }

        public UpgradeEntry RollEntryFromSubList(UpgradeEntry[] list, NetworkRandom nr, int min, DateTime date, ref string log)
        {
            UpgradeEntry r = null;
            CalculateLimit(list, date);
            float rand = nr.Float(min < 0 ? 0f : list[min].RandomLimit, 1f);
            for (int i = 0; i < list.Length; i++)
            {
                UpgradeEntry u = list[i];
                if (rand <= u.RandomLimit && CheckUpgradeCond(u, date))
                    r = u;
            }
            log += $" -> {r.ID} ({rand})";
            if (r.ListLink)
            {
                UpgradeEntry li = RollEntryFromMatchingSubList(r.ID, nr, date, ref log);
                if (li != null)
                    r = li;
            }
            return r;
        }

        public int CompareTo(UpgradeList other)
        {
            return this.Sort - other.Sort;
        }

        public class UpgradeEntry
        {
            public string ID = null;
            public int Weight = 0;
            public float RandomLimit = 0;
            public DateTime MinDate = DateTime.MinValue;
            public bool ListLink = false;
            public bool AllowDowngrade = false;
        }
    }
}
