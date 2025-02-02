using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public class UpgradeSubList
    {
        public UpgradeEntry[] MainUpgradePath;
        public UpgradeEntry[] AmmoTypes;
        public UpgradeEntry[] Addons;
        [JsonIgnore]
        public string Name { get; internal set; }

        internal void CalculateLimit(DateTime d, int[] wlt, SubListType t)
        {
            UpgradeEntry[] l = Get(t);
            float cw = l.Sum(u => u.GetWeight(d, wlt));
            float last = 0;
            foreach (UpgradeEntry u in l)
            {
                last += u.GetWeight(d, wlt) / cw;
                u.RandomLimit = last;
            }
        }

        public UpgradeEntry[] Get(SubListType t)
        {
            switch (t)
            {
                case SubListType.Main:
                    return MainUpgradePath;
                case SubListType.Ammo:
                    return AmmoTypes;
                case SubListType.Addon:
                    return Addons;
                default:
                    return Array.Empty<UpgradeEntry>();
            }
        }
    }

    public enum SubListType
    {
        Main,
        Ammo,
        Addon,
    }
}
