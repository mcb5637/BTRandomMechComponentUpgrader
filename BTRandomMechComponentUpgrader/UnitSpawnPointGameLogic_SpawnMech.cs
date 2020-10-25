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
        private static ChassisLocations[] Locations = new ChassisLocations[] { ChassisLocations.CenterTorso, ChassisLocations.Head,
            ChassisLocations.RightArm, ChassisLocations.RightLeg, ChassisLocations.RightTorso,
            ChassisLocations.LeftArm, ChassisLocations.LeftLeg, ChassisLocations.LeftTorso};

        public static void Prefix(UnitSpawnPointGameLogic __instance, ref MechDef mDef, Team team)
        {
            BTRandomMechComponentUpgrader_Init.Log.Log("called");
            SimGameState s = __instance.Combat.BattleTechGame.Simulation;
            if (s == null)
                return;
            if (s.DataManager.MechDefs.TryGet(mDef.Description.Id, out MechDef m))
            {
                if (mDef == m) // if its a player mech, it is a different mechdef than in the datamanager
                {
                    BTRandomMechComponentUpgrader_UpgradeList ulist = GetUpgradeList(team); // check if we got a upgradelist for that faction
                    BTRandomMechComponentUpgrader_Init.Log.Log($"selected ulist {(ulist == null ? "null" : ulist.Name)}");
                    if (ulist == null)
                        return;

                    BTRandomMechComponentUpgrader_Init.Log.Log($"upgrading {mDef.Description.Name} {mDef.Chassis.VariantName}");

                    mDef = new MechDef(mDef); // dont break mechdefs in datamanager

                    float canFreeTonns = Mathf.Floor(mDef.Inventory.Sum((r) => ulist.CanRemove.Contains(r.ComponentDefID) ? r.Def.Tonnage : 0f) * ulist.RemoveMaxFactor);


                    // check standard upgrade
                    CheckUpgrades(mDef, s, ulist, ref canFreeTonns);

                    List<MechComponentRef> inv = mDef.Inventory.ToList();

                    // check additions
                    CheckAdditions(mDef, s, ulist, inv, ref canFreeTonns);

                    // fix tonnage (remove or add heatsinks)
                    mDef.SetInventory(inv.ToArray());
                    CorrectTonnage(mDef, s, ulist, inv);

                    mDef.SetInventory(inv.ToArray());
                    BTRandomMechComponentUpgrader_Init.Log.Log("finished, inv dump:");
                    foreach (MechComponentRef r in mDef.Inventory)
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log($"inventory {r.ComponentDefID} at {r.MountedLocation} {r.HardpointSlot}");
                    }
                    BTRandomMechComponentUpgrader_Init.Log.Log("done");

                    if (!ValidateMech(mDef, s))
                        mDef = m;
                }
            }
        }

        private static void CheckAdditions(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, List<MechComponentRef> inv, ref float canFreeTonns)
        {
            foreach (BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[] l in ulist.Additions)
            {
                if (s.NetworkRandom.Float(0f, 1f) < ulist.UpgradePerComponentChance)
                {
                    int repeatResult = -1;
                    string sel = BTRandomMechComponentUpgrader_UpgradeList.GetUpgradeFromRandom(l, s.NetworkRandom.Float(0f, 1f), s.CurrentDate, out bool _, ref repeatResult, out string _, out string _);
                    if (sel != null)
                    {
                        MechComponentDef d = GetComponentDefFromID(s, sel);
                        ChassisLocations loc = ChassisLocations.None;
                        foreach (ChassisLocations lo in Locations)
                        {
                            if (CanUpgrade(null, d, canFreeTonns, mDef, lo) && CanPutComponentIntoLoc(d, lo))
                            {
                                loc = lo;
                                break;
                            }
                        }
                        if (loc == ChassisLocations.None)
                            continue;
                        BTRandomMechComponentUpgrader_Init.Log.Log($"adding {sel} into {loc}");
                        MechComponentRef r = new MechComponentRef(sel, null, d.ComponentType, loc, -1, ComponentDamageLevel.Functional, false);
                        r.SetComponentDef(d);
                        inv.Add(r);
                    }
                }
            }
        }

        private static void CorrectTonnage(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, List<MechComponentRef> inv)
        {
            float tonnage = 0;
            float max = 0;
            MechStatisticsRules.CalculateTonnage(mDef, ref tonnage, ref max);
            while (tonnage > mDef.Chassis.Tonnage)
            {
                int i = inv.FindIndex((x) => ulist.CanRemove.Contains(x.ComponentDefID));
                if (i == -1)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("no removable found");
                    break;
                }
                BTRandomMechComponentUpgrader_Init.Log.Log($"removed {inv[i].ComponentDefID} to reduce weight");
                tonnage -= inv[i].Def.Tonnage;
                inv.RemoveAt(i);
            }
            MechComponentDef fill1ton = null;
            MechComponentDef fill05ton = null;
            foreach (string id in ulist.CanRemove)
            {
                MechComponentDef d = GetComponentDefFromID(s, id);
                if (fill1ton == null && d.Tonnage == 1)
                    fill1ton = d;
                if (fill05ton == null && d.Tonnage == 0.5f)
                    fill05ton = d;
            }
            while (tonnage + 0.5 <= mDef.Chassis.Tonnage)
            {
                MechComponentDef d = fill05ton;
                if (tonnage + 1 <= mDef.Chassis.Tonnage)
                    d = fill1ton;
                ChassisLocations loc = ChassisLocations.None;
                foreach (ChassisLocations l in Locations)
                {
                    if (GetFreeSlotsInLoc(mDef, inv, l, null) >= d.InventorySize && CanPutComponentIntoLoc(d, l))
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
                BTRandomMechComponentUpgrader_Init.Log.Log($"added {r.ComponentDefID} to use free weight");
            }
            BTRandomMechComponentUpgrader_Init.Log.Log($"final weight: {tonnage}/{mDef.Chassis.Tonnage}");
        }

        private static void CheckUpgrades(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, ref float canFreeTonns)
        {
            Dictionary<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[], int> repeatUpgradeResults = new Dictionary<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[], int>();
            foreach (MechComponentRef r in mDef.Inventory)
            {
                try
                {
                    CheckForAndPerformUpgrade(r, s, ulist, ref canFreeTonns, mDef, repeatUpgradeResults);
                }
                catch (Exception e)
                {
                    BTRandomMechComponentUpgrader_Init.Log.LogException(e);
                }
            }
        }

        public static BTRandomMechComponentUpgrader_UpgradeList GetUpgradeList(Team team)
        {
            BTRandomMechComponentUpgrader_Init.Log.Log($"searching UpgradeList for faction {team.FactionValue}");
            foreach (BTRandomMechComponentUpgrader_UpgradeList l in BTRandomMechComponentUpgrader_Init.UpgradeLists)
            {
                if (l.DoesApplyToFaction(team.FactionValue.ToString()))
                    return l;
            }
            return null;
        }

        public static void CheckForAndPerformUpgrade(MechComponentRef r, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList l, ref float canFreeTonns, MechDef mech, Dictionary<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[], int> repeatResults)
        {
            if (l == null)
                return;
            BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[] le = l.GetUpgradeArrayAndOffset(r.Def.Description.Id, out float minr);
            if (le != null)
            {
                int repeat = -2;
                if (repeatResults.ContainsKey(le))
                    repeat = repeatResults[le];

                if (repeat<0 && s.NetworkRandom.Float(0f, 1f) > l.UpgradePerComponentChance) // roll if upgrade only if no repeat is saved
                    return;

                string sel = BTRandomMechComponentUpgrader_UpgradeList.GetUpgradeFromRandom(le, s.NetworkRandom.Float(minr, 1f), s.CurrentDate, out bool ReCheckUpgrade, ref repeat, out string swapAmmoFrom, out string swapAmmoTo);
                if (sel != null)
                {
                    if (repeat <= -2)
                        repeat = -1;
                    repeatResults[le] = repeat;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"changing {r.ComponentDefID} -> {sel}");
                    MechComponentDef d = GetComponentDefFromID(s, sel);
                    if (!CanUpgrade(r, d, canFreeTonns, mech, r.MountedLocation))
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log("cannot upgrade");
                        return;
                    }
                    DoUpgrade(r, d, ref canFreeTonns);
                    if (swapAmmoFrom!=null)
                    {
                        MechComponentDef am = GetComponentDefFromID(s, swapAmmoTo);
                        foreach (MechComponentRef re in mech.Inventory)
                        {
                            if (re.ComponentDefID.Equals(swapAmmoFrom))
                            {
                                BTRandomMechComponentUpgrader_Init.Log.Log($"changing ammo {re.ComponentDefID} -> {swapAmmoTo}");
                                DoUpgrade(re, am, ref canFreeTonns);
                            }
                        }
                    }
                    if (ReCheckUpgrade && s.NetworkRandom.Float(0f, 1f) < l.UpgradePerComponentChance)
                        CheckForAndPerformUpgrade(r, s, l, ref canFreeTonns, mech, repeatResults);
                }
                else
                    BTRandomMechComponentUpgrader_Init.Log.Log($"found no upgrade for {r.ComponentDefID}");
            }
        }

        public static MechComponentDef GetComponentDefFromID(SimGameState s, string id)
        {
            if (s.DataManager.AmmoBoxDefs.TryGet(id, out AmmunitionBoxDef adef))
                return adef;
            if (s.DataManager.HeatSinkDefs.TryGet(id, out HeatSinkDef hdef))
                return hdef;
            if (s.DataManager.JumpJetDefs.TryGet(id, out JumpJetDef jdef))
                return jdef;
            if (s.DataManager.UpgradeDefs.TryGet(id, out UpgradeDef udef))
                return udef;
            if (s.DataManager.WeaponDefs.TryGet(id, out WeaponDef wdef))
                return wdef;
            return null;
        }

        public static void DoUpgrade(MechComponentRef r, MechComponentDef d, ref float canFreeTonns)
        {
            canFreeTonns += r.Def.Tonnage;
            r.ComponentDefID = d.Description.Id;
            //Traverse.Create(r).Property("Def").SetValue(wdef);
            r.SetComponentDef(d);
            canFreeTonns -= r.Def.Tonnage;
        }

        public static bool CanUpgrade(MechComponentRef r, MechComponentDef d, float canFreeTonns, MechDef mech, ChassisLocations loc)
        {
            if (d == null)
                return false;
            if (r != null)
            {
                canFreeTonns += r.Def.Tonnage;
            }
            if (canFreeTonns < d.Tonnage)
                return false;
            int slots = GetFreeSlotsInLoc(mech, null, loc, r);
            if (slots < d.InventorySize)
                return false;
            return true;
        }

        public static int GetFreeSlotsInLoc(MechDef mech, IEnumerable<MechComponentRef> inv, ChassisLocations loc, MechComponentRef except)
        {
            int slots = mech.GetChassisLocationDef(loc).InventorySlots;
            if (inv == null)
                inv = mech.Inventory;
            foreach (MechComponentRef i in inv)
                if (i.MountedLocation == loc && i != except)
                    slots -= i.Def.InventorySize;
            return slots;
        }

        public static bool CanPutComponentIntoLoc(MechComponentDef d, ChassisLocations loc)
        {
            return (d.AllowedLocations & loc) != ChassisLocations.None;
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
