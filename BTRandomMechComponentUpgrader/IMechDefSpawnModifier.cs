using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    interface IMechDefSpawnModifier
    {
        void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, List<string[]> changedAmmoTypes);
    }
}
