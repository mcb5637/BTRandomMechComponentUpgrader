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

        internal void CalculateLimit(DateTime d, int[] wlt)
        {
            float cw = MainUpgradePath.Sum(u => u.GetWeight(d, wlt));
            float last = 0;
            foreach (UpgradeEntry u in MainUpgradePath)
            {
                last += u.GetWeight(d, wlt) / cw;
                u.RandomLimit = last;
            }
        }
    }
}
