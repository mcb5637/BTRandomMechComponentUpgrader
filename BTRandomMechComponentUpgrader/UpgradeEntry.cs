using System;

namespace BTRandomMechComponentUpgrader
{
    public class UpgradeEntry
    {
        public string ID = null;
        public int Weight = 0;
        public float RandomLimit = 0;
        public DateTime MinDate = DateTime.MinValue;
        public bool ListLink = false;
        public bool AllowDowngrade = false;
        public string[] AmmoLockoutByAddon = Array.Empty<string>();
        public int AmmoWeight = 1;


        public bool CheckUpgradeCond(DateTime d)
        {
            if (MinDate > d)
                return false;
            return true;
        }

        public int GetWeight(DateTime d, int[] wlt)
        {
            if (!CheckUpgradeCond(d))
                return 0;
            if (wlt != null)
                return wlt[Math.Max(Math.Min(Weight, wlt.Length), 0)];
            return Weight;
        }
    }
}
