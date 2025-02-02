using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public class UpgradeList : IComparable<UpgradeList>
    {
        public List<UpgradeSubList> Upgrades = new List<UpgradeSubList>();
        public string[] Factions = new string[] { };
        public string[] FactionPrefixWithNumber = new string[] { };
        public float UpgradePerComponentChance = 0.5f;
        public string[] CanRemove = new string[] { };
        public float RemoveMaxFactor = 0.5f;
        public List<UpgradeSubList> Additions = new List<UpgradeSubList>();
        public bool AllowDowngrade = false;
        public string[] LoadUpgrades = new string[] { };
        public string[] LoadAdditions = new string[] { };
        public int[] WeightLookupTable = null;
        [JsonIgnore]
        public string Name { get; internal set; }
        public int Sort = 0;

        public bool DoesApplyToFaction(string fac)
        {
            return Factions.Contains("default_all") || Factions.Contains(fac) || FactionPrefixWithNumber.Any((x) => StartWithAndRestIsNumeric(fac, x));
        }
        public static bool StartWithAndRestIsNumeric(string test, string pre)
        {
            if (test.Length <= pre.Length)
                return false;
            if (!test.StartsWith(pre))
                return false;
            return test.Skip(pre.Length).All(char.IsDigit);
        }

        public UpgradeSubList GetUpgradeSubListAndOffset(string comp, SubListType t, out int min)
        {
            foreach (UpgradeSubList list in Upgrades)
            {
                if (IsInUpgradeArray(comp, list.Get(t), out min))
                    return list;
            }
            min = -1;
            return null;
        }

        public bool IsInUpgradeArray(string comp, UpgradeEntry[] list, out int min)
        {
            for (int i = 0; i < list.Length; i++)
            {
                UpgradeEntry u = list[i];
                if (u.ID.Equals(comp) && !u.ListLink)
                {
                    min = (u.AllowDowngrade || this.AllowDowngrade) ? -1 : i;
                    return true;
                }
            }
            min = -1;
            return false;
        }

        public UpgradeEntry RollEntryFromMatchingSubList(string baseid, NetworkRandom nr, DateTime date, SubListType t, ref string log, float linkRerollChance)
        {
            UpgradeSubList sublist = GetUpgradeSubListAndOffset(baseid, t, out int min);
            UpgradeEntry r = null;
            if (sublist != null)
            {
                r = RollEntryFromSubList(sublist, nr, min, date, t, ref log, linkRerollChance);
            }
            else
                log += " no sublist found";
            return r;
        }

        public UpgradeEntry RollEntryFromSubList(UpgradeSubList list, NetworkRandom nr, int min, DateTime date, SubListType t, ref string log, float linkRerollChance)
        {
            UpgradeEntry r = null;
            UpgradeEntry[] entries = list.Get(t);
            if (entries == null)
                return null;
            list.CalculateLimit(date, WeightLookupTable, t);
            float rand = nr.Float(min < 0 ? 0f : entries[min].RandomLimit, 1f);
            for (int i = 0; i < entries.Length; i++)
            {
                UpgradeEntry u = entries[i];
                if (rand <= u.RandomLimit && u.CheckUpgradeCond(date))
                {
                    r = u;
                    break;
                }
            }
            log += $" -> {r.ID} ({rand}, {min}, {list.Name})";
            if (r.ListLink && nr.Float(0f, 1f) <= linkRerollChance)
            {
                UpgradeEntry li = RollEntryFromMatchingSubList(r.ID, nr, date, SubListType.Main, ref log, linkRerollChance);
                if (li != null)
                    r = li;
            }
            return r;
        }

        public int CompareTo(UpgradeList other)
        {
            return this.Sort - other.Sort;
        }
    }
}
