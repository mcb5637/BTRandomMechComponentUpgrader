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


        internal bool CheckUpgradeCond(DateTime d)
        {
            if (MinDate > d)
                return false;
            return true;
        }
    }
}
