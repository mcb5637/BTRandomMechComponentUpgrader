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
        public static void Prefix(UnitSpawnPointGameLogic __instance, ref MechDef mDef, Team team)
        {
            try
            {
                SimGameState s = __instance.Combat.BattleTechGame.Simulation;
                if (s == null)
                {
                    Main.Log.Log("no simgame, aborting");
                    return;
                }

                if (mDef.Chassis.ChassisTags.Contains("deploy_director"))
                {
                    Main.Log.Log($"DeployDirector, not upgrading");
                    return;
                }
                
                if (!s.DataManager.MechDefs.TryGet(mDef.Description.Id, out MechDef m))
                {
                    Main.Log.Log($"no mechdef found in datamanager for {mDef.Description.Id}");
                    return;
                }

                if (mDef != m) // if its a player mech, it is a different mechdef than in the datamanager)
                {
                    Main.Log.Log("player modified mech, no upgrading");
                    return;
                }

                FactionValue teamName = team.IsLocalPlayer ? __instance.Combat.ActiveContract.Override.employerTeam.FactionValue : team.FactionValue;
                UpgradeList ulist = MechProcessor.GetUpgradeList(teamName); // check if we got a upgradelist for that faction
                if (ulist == null)
                {
                    Main.Log.Log($"no UpgradeList found for {teamName}");
                    return;
                }

                MechDef n = MechProcessor.ProcessMech(mDef, s, ulist);

                if (MechProcessor.ValidateMech(n, s))
                {
                    mDef = n;
                    Main.Log.Log("validated");
                }
                else
                {
                    Main.Log.Log("validation failed, using datamanager mechdef, inventory dump of invalid mechdef:");
                    foreach (MechComponentRef r in n.Inventory)
                    {
                        Main.Log.Log($"inventory {r.ComponentDefID} at {r.MountedLocation} {r.HardpointSlot}");
                    }
                }
            }
            catch (Exception e)
            {
                Main.Log.LogException(e);
            }
        }
    }
}
