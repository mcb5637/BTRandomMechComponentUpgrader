using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public class Modifier_TonnageFixArmor : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float _, AmmoTracker changedAmmoTypes, MechDef fromData, FactionValue team)
        {
            float tonnage = 0;
            float max = 0;
            MechStatisticsRules.CalculateTonnage(mDef, ref tonnage, ref max);
            float armorfact = mDef.GetMechArmorPointFactor();
            Main.Log.Log($"correcting tonnage 2: armor (each armor point costs {armorfact} t)");
            while (tonnage + armorfact <= mDef.Chassis.Tonnage)
            {
                bool assOne = false;
                foreach (ChassisLocations c in RMCU_Helper.Locations)
                {
                    if (tonnage + armorfact >= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedArmor >= mDef.GetChassisLocationDef(c).MaxArmor)
                        continue;
                    l.AssignedArmor += 1;
                    l.CurrentArmor += 1;
                    tonnage += armorfact;
                    Main.Log.Log($"increased {c} armor to {l.AssignedArmor}");
                    assOne = true;
                }
                foreach (ChassisLocations c in RMCU_Helper.RearArmoredLocs)
                {
                    if (tonnage + armorfact >= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedRearArmor >= mDef.GetChassisLocationDef(c).MaxRearArmor)
                        continue;
                    l.AssignedRearArmor += 1;
                    l.CurrentRearArmor += 1;
                    tonnage += armorfact;
                    Main.Log.Log($"increased {c} rear armor to {l.AssignedRearArmor}");
                    assOne = true;
                }
                if (!assOne)
                {
                    Main.Log.Log("no free armor location found!");
                    break;
                }
            }
            while (tonnage > mDef.Chassis.Tonnage)
            {
                bool assOne = false;
                foreach (ChassisLocations c in RMCU_Helper.Locations)
                {
                    if (tonnage <= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedArmor <= 1)
                        continue;
                    l.AssignedArmor -= 1;
                    l.CurrentArmor -= 1;
                    tonnage -= armorfact;
                    Main.Log.Log($"decreased {c} armor to {l.AssignedArmor}");
                    assOne = true;
                }
                foreach (ChassisLocations c in RMCU_Helper.RearArmoredLocs)
                {
                    if (tonnage <= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedRearArmor <= 1)
                        continue;
                    l.AssignedRearArmor -= 1;
                    l.CurrentRearArmor -= 1;
                    tonnage -= armorfact;
                    Main.Log.Log($"decreased {c} rear armor to {l.AssignedRearArmor}");
                    assOne = true;
                }
                if (!assOne)
                {
                    Main.Log.Log("no free armor location found!");
                    break;
                }
            }

            Main.Log.Log($"final weight: {tonnage}/{mDef.Chassis.Tonnage}");
        }
    }
}
