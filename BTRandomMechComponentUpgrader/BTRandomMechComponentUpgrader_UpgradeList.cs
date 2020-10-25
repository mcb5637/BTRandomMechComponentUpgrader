using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class BTRandomMechComponentUpgrader_UpgradeList : IComparable<BTRandomMechComponentUpgrader_UpgradeList>
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


        public void CalculateLimits()
        {
            foreach (UpgradeEntry[] ut in Upgrades)
            {
                CalculateLimit(ut);
            }
            foreach (UpgradeEntry[] ut in Additions)
            {
                CalculateLimit(ut);
            }
        }

        private static void CalculateLimit(UpgradeEntry[] ut)
        {
            float cw = ut.Sum((u) => u.Weight);
            float last = 0;
            foreach (UpgradeEntry u in ut)
            {
                last += u.Weight / cw;
                u.RandomLimit = last;
            }
        }

        public bool DoesApplyToFaction(string fac)
        {
            return Factions.Contains("default_all") || Factions.Contains(fac);
        }

        public UpgradeEntry[] GetUpgradeArrayAndOffset(string comp, out float minr)
        {
            foreach (UpgradeEntry[] list in Upgrades)
            {
                foreach (UpgradeEntry u in list)
                {
                    if (u.ID.Equals(comp) && !u.ListLink)
                    {
                        minr = (u.AllowDowngrade || this.AllowDowngrade) ? 0 : u.RandomLimit;
                        return list;
                    }
                }
            }
            minr = -1f;
            return null;
        }

        public static string GetUpgradeFromRandom(UpgradeEntry[] list, float r, DateTime date, out bool ReCheckUpgrade, ref int repeatUpgradeResult, out string swapAmmoFrom, out string swapAmmoTo)
        {
            if (repeatUpgradeResult >= 0)
            {
                UpgradeEntry u = list[repeatUpgradeResult];
                ReCheckUpgrade = u.ListLink;
                swapAmmoFrom = u.SwapAmmoFrom;
                swapAmmoTo = u.SwapAmmoTo;
                return u.ID;
            }
            for (int i = 0; i < list.Length; i++)
            {
                UpgradeEntry u = list[i];
                if (r < u.RandomLimit && u.MinDate <= date && (!u.UpgradeAll || repeatUpgradeResult<=-2))
                {
                    ReCheckUpgrade = u.ListLink;
                    swapAmmoFrom = u.SwapAmmoFrom;
                    swapAmmoTo = u.SwapAmmoTo;
                    if (u.UpgradeAll)
                        repeatUpgradeResult = i;
                    return u.ID;
                }
            }
            ReCheckUpgrade = false;
            swapAmmoFrom = null;
            swapAmmoTo = null;
            return null;
        }

        public int CompareTo(BTRandomMechComponentUpgrader_UpgradeList other)
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
            public bool UpgradeAll = false;
            public string SwapAmmoFrom = null;
            public string SwapAmmoTo = null;
            public bool AllowDowngrade = false;
        }
    }
}
