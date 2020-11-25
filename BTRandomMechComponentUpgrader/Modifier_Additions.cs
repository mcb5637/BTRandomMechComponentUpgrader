using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class Modifier_Additions : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, List<string[]> changedAmmoTypes)
        {
            BTRandomMechComponentUpgrader_Init.Log.Log("checking addition sublists");
            List<MechComponentRef> inv = mDef.Inventory.ToList();
            foreach (UpgradeList.UpgradeEntry[] l in ulist.Additions)
            {
                if (s.NetworkRandom.Float(0f, 1f) < ulist.UpgradePerComponentChance)
                {
                    string log = "";
                    UpgradeList.UpgradeEntry ue = ulist.RollEntryFromSubList(l, s.NetworkRandom, -1, s.CurrentDate, ref log, ulist.UpgradePerComponentChance);
                    if (ue != null && !ue.ID.Equals(""))
                    {
                        MechComponentDef d = s.GetComponentDefFromID(ue.ID);
                        ChassisLocations loc = ChassisLocations.None;
                        foreach (ChassisLocations lo in RMCU_Helper.Locations)
                        {
                            if (RMCU_Helper.CanUpgrade(null, d, canFreeTonns, mDef, lo, inv) && d.CanPutComponentIntoLoc(lo))
                            {
                                loc = lo;
                                break;
                            }
                        }
                        if (loc == ChassisLocations.None)
                        {
                            BTRandomMechComponentUpgrader_Init.Log.Log("cannot add " + log);
                            continue;
                        }
                        BTRandomMechComponentUpgrader_Init.Log.Log($"adding {log} into {loc}");
                        MechComponentRef r = new MechComponentRef(ue.ID, null, d.ComponentType, loc, -1, ComponentDamageLevel.Functional, false);
                        r.SetComponentDef(d);
                        inv.Add(r);
                    }
                    else
                        BTRandomMechComponentUpgrader_Init.Log.Log("cannot add, nothing rolled " + log);
                }
            }
            mDef.SetInventory(inv.ToArray());
        }
    }
}
