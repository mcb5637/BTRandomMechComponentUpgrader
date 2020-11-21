using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class RMCU_Modifier_TonnageFixInventory : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, ref float _)
        {
            BTRandomMechComponentUpgrader_Init.Log.Log("correcting tonage 1: inventory");
            List<MechComponentRef> inv = mDef.Inventory.ToList();
            float tonnage = 0;
            float max = 0;
            MechStatisticsRules.CalculateTonnage(mDef, ref tonnage, ref max);
            while (tonnage > mDef.Chassis.Tonnage)
            {
                int i = inv.FindIndex((x) => !x.IsFixed && ulist.CanRemove.Contains(x.ComponentDefID));
                if (i == -1)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("no removable found");
                    break;
                }
                BTRandomMechComponentUpgrader_Init.Log.Log($"removed {inv[i].ComponentDefID} to reduce weight");
                tonnage -= inv[i].Def.Tonnage;
                inv.RemoveAt(i);
            }

            foreach (string id in ulist.CanRemove)
            {
                MechComponentDef d = s.GetComponentDefFromID(id);
                while (tonnage + d.Tonnage <= mDef.Chassis.Tonnage)
                {
                    ChassisLocations loc = ChassisLocations.None;
                    foreach (ChassisLocations l in RMCU_Helper.Locations)
                    {
                        if (mDef.GetFreeSlotsInLoc( inv, l, null) >= d.InventorySize && d.CanPutComponentIntoLoc(l))
                        {
                            loc = l;
                            break;
                        }
                    }
                    if (loc == ChassisLocations.None)
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log("no free location found!");
                        break;
                    }
                    MechComponentRef r = new MechComponentRef(d.Description.Id, null, d.ComponentType, loc, -1, ComponentDamageLevel.Functional, false);
                    r.SetComponentDef(d);
                    inv.Add(r);
                    tonnage += d.Tonnage;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"added {r.ComponentDefID} to use free weight");
                }
            }
            mDef.SetInventory(inv.ToArray());
        }
    }
}
