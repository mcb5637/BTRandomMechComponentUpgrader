using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public class Modifier_Upgrades : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, AmmoTracker changedAmmoTypes, MechDef fromData, FactionValue team)
        {
            Main.Log.Log("checking upgrade sublists");
            foreach (MechComponentRef r in mDef.Inventory)
            {
                if (r.IsFixed)
                    continue;
                CheckForAndPerformUpgrade(r, s, ulist, ref canFreeTonns, mDef, changedAmmoTypes);
            }
        }

        private void CheckForAndPerformUpgrade(MechComponentRef r, SimGameState s, UpgradeList l, ref float canFreeTonns, MechDef mech, AmmoTracker changedAmmoTypes)
        {
            string baseid = r.Def.Description.Id;
            if (s.NetworkRandom.Float(0f, 1f) > l.UpgradePerComponentChance)
                return;

            string log = baseid;
            UpgradeEntry ue = l.RollEntryFromMatchingSubList(baseid, s.NetworkRandom, s.CurrentDate, SubListType.Main, ref log, l.UpgradePerComponentChance, out UpgradeSubList rootSubList, out UpgradeSubList lastSubList);
            if (ue != null)
            {
                MechComponentDef d = s.GetComponentDefFromID(ue.ID);
                if (r.CanUpgrade(d, canFreeTonns, mech, r.MountedLocation, mech.Inventory))
                {
                    changedAmmoTypes.OnChange(r.Def, d, rootSubList, lastSubList);
                    r.DoUpgrade(d, ref canFreeTonns);
                    Main.Log.Log("changing " + log);
                }
                else
                    Main.Log.Log("cannot upgrade " + log);
            }
            else
                Main.Log.Log("upgrade unavailable " + log);
        }
    }
}
