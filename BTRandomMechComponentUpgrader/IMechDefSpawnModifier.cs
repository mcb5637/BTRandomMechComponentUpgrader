using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public interface IMechDefSpawnModifier
    {
        void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, AmmoTracker changedAmmoTypes, MechDef fromData, FactionValue team);
    }
}
