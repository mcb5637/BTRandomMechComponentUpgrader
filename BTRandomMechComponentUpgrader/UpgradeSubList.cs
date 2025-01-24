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

        internal void CalculateLimit(DateTime d)
        {
            float cw = MainUpgradePath.Sum((u) => u.CheckUpgradeCond(d) ? u.Weight : 0);
            float last = 0;
            foreach (UpgradeEntry u in MainUpgradePath)
            {
                if (u.CheckUpgradeCond(d))
                    last += u.Weight / cw;
                u.RandomLimit = last;
            }
        }
    }
}
