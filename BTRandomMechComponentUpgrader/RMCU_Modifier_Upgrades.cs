using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class RMCU_Modifier_Upgrades : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, ref float canFreeTonns)
        {
            BTRandomMechComponentUpgrader_Init.Log.Log("checking upgrade sublists");
            foreach (MechComponentRef r in mDef.Inventory)
            {
                if (r.IsFixed)
                    continue;
                CheckForAndPerformUpgrade(r, s, ulist, ref canFreeTonns, mDef);
            }
        }

        public static void CheckForAndPerformUpgrade(MechComponentRef r, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList l, ref float canFreeTonns, MechDef mech)
        {
            string baseid = r.Def.Description.Id;
            if (s.NetworkRandom.Float(0f, 1f) > l.UpgradePerComponentChance) // roll if upgrade only if no repeat is saved
                return;

            string log = baseid;
            BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry ue = l.RollEntryFromMatchingSubList(baseid, s.NetworkRandom, s.CurrentDate, ref log);
            if (ue != null)
            {
                MechComponentDef d = s.GetComponentDefFromID(ue.ID);
                if (r.CanUpgrade(d, canFreeTonns, mech, r.MountedLocation, mech.Inventory))
                {
                    r.DoUpgrade(d, ref canFreeTonns);
                    BTRandomMechComponentUpgrader_Init.Log.Log("changing " + log);
                }
                else
                    BTRandomMechComponentUpgrader_Init.Log.Log("cannot upgrade " + log);
            }
            else
                BTRandomMechComponentUpgrader_Init.Log.Log("upgrade unavailable " + log);
        }

    }
}
