using BattleTech;
using BattleTech.Data;
using Harmony;
using UnityEngine;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "SpawnMech")]
    class UnitSpawnPointGameLogic_SpawnMech
    {
        private static readonly ChassisLocations[] Locations = new ChassisLocations[] { ChassisLocations.CenterTorso, ChassisLocations.Head,
            ChassisLocations.RightArm, ChassisLocations.RightLeg, ChassisLocations.RightTorso,
            ChassisLocations.LeftArm, ChassisLocations.LeftLeg, ChassisLocations.LeftTorso};
        private static readonly ChassisLocations[] RearArmoredLocs = new ChassisLocations[] { ChassisLocations.CenterTorso, ChassisLocations.RightTorso, ChassisLocations.LeftTorso };

        public static void Prefix(UnitSpawnPointGameLogic __instance, ref MechDef mDef, Team team)
        {
            try
            {
                SimGameState s = __instance.Combat.BattleTechGame.Simulation;
                if (s == null)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("no simgame, aborting");
                    return;
                }
                
                if (s.DataManager.MechDefs.TryGet(mDef.Description.Id, out MechDef m))
                {
                    if (mDef == m) // if its a player mech, it is a different mechdef than in the datamanager
                    {
                        BTRandomMechComponentUpgrader_UpgradeList ulist = GetUpgradeList(team.IsLocalPlayer ? __instance.Combat.ActiveContract.Override.employerTeam.FactionValue.ToString() : team.FactionValue.ToString()); // check if we got a upgradelist for that faction
                        BTRandomMechComponentUpgrader_Init.Log.Log($"selected ulist {(ulist == null ? "null" : ulist.Name)}");
                        if (ulist == null)
                            return;

                        BTRandomMechComponentUpgrader_Init.Log.Log($"upgrading {mDef.Description.Name} {mDef.Chassis.VariantName}");

                        MechDef n = new MechDef(mDef); // dont break mechdefs in datamanager

                        float canFreeTonns = Mathf.Floor(n.Inventory.Sum((r) => ulist.CanRemove.Contains(r.ComponentDefID) ? r.Def.Tonnage : 0f) * ulist.RemoveMaxFactor);

                        foreach (IMechDefSpawnModifier mod in BTRandomMechComponentUpgrader_Init.SpawnModifiers)
                            mod.ModifyMech(n, s, ulist, ref canFreeTonns);

                        if (ValidateMech(n, s))
                        {
                            mDef = n;
                            BTRandomMechComponentUpgrader_Init.Log.Log("done");
                        }
                        else
                        {
                            BTRandomMechComponentUpgrader_Init.Log.Log("validation failed, inventory dump:");
                            foreach (MechComponentRef r in n.Inventory)
                            {
                                BTRandomMechComponentUpgrader_Init.Log.Log($"inventory {r.ComponentDefID} at {r.MountedLocation} {r.HardpointSlot}");
                            }
                        }
                    }
                    else
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log("player modified mech, no upgrading");
                    }
                }
                else
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log($"no mechdef found in datamanager for {mDef.Description.Id}");
                }
            }
            catch (Exception e)
            {
                BTRandomMechComponentUpgrader_Init.Log.LogException(e);
            }
        }

        public static BTRandomMechComponentUpgrader_UpgradeList GetUpgradeList(string team)
        {
            BTRandomMechComponentUpgrader_Init.Log.Log($"searching UpgradeList for faction {team}");
            foreach (BTRandomMechComponentUpgrader_UpgradeList l in BTRandomMechComponentUpgrader_Init.UpgradeLists)
            {
                if (l.DoesApplyToFaction(team))
                    return l;
            }
            return null;
        }

        public static bool ValidateMech(MechDef m, SimGameState s)
        {
            bool r = true;
            foreach (KeyValuePair<MechValidationType, List<Text>> kv in MechValidationRules.ValidateMechDef(MechValidationLevel.Full, s.DataManager, m, null))
            {
                foreach (Text t in kv.Value)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log($"validation error: {t}");
                    r = false;
                }
            }
            return r;
        }
    }
}
