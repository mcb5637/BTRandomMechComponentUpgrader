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
                    BTRandomMechComponentUpgrader_Init.Log.Log("no simgame, aborting");
                    return;
                }

                if (mDef.Chassis.ChassisTags.Contains("deploy_director"))
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log($"DeployDirector, not upgrading");
                    return;
                }
                
                if (!s.DataManager.MechDefs.TryGet(mDef.Description.Id, out MechDef m))
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log($"no mechdef found in datamanager for {mDef.Description.Id}");
                    return;
                }

                if (mDef != m) // if its a player mech, it is a different mechdef than in the datamanager)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("player modified mech, no upgrading");
                    return;
                }

                string teamName = team.IsLocalPlayer ? __instance.Combat.ActiveContract.Override.employerTeam.FactionValue.ToString() : team.FactionValue.ToString();
                UpgradeList ulist = MechProcesser.GetUpgradeList(teamName); // check if we got a upgradelist for that faction
                if (ulist == null)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log($"no UpgradeList found for {teamName}");
                    return;
                }

                MechDef n = MechProcesser.ProcessMech(mDef, s, ulist);

                if (MechProcesser.ValidateMech(n, s))
                {
                    mDef = n;
                    BTRandomMechComponentUpgrader_Init.Log.Log("validated");
                }
                else
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("validation failed, using datamanager mechdef, inventory dump of invalid mechdef:");
                    foreach (MechComponentRef r in n.Inventory)
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log($"inventory {r.ComponentDefID} at {r.MountedLocation} {r.HardpointSlot}");
                    }
                }
            }
            catch (Exception e)
            {
                BTRandomMechComponentUpgrader_Init.Log.LogException(e);
            }
        }
    }
}
