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


                        // check standard upgrade
                        CheckUpgrades(n, s, ulist, ref canFreeTonns);

                        // check additions
                        CheckAdditions(n, s, ulist, ref canFreeTonns);

                        // fix tonnage (remove or add heatsinks/armor)
                        CorrectTonnage(n, s, ulist);

                        BTRandomMechComponentUpgrader_Init.Log.Log("finished, inv dump:");
                        foreach (MechComponentRef r in n.Inventory)
                        {
                            BTRandomMechComponentUpgrader_Init.Log.Log($"inventory {r.ComponentDefID} at {r.MountedLocation} {r.HardpointSlot}");
                        }
                        BTRandomMechComponentUpgrader_Init.Log.Log("done");

                        if (ValidateMech(n, s))
                            mDef = n;
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

        private static void CheckAdditions(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, ref float canFreeTonns)
        {
            List<MechComponentRef> inv = mDef.Inventory.ToList();
            foreach (BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[] l in ulist.Additions)
            {
                if (s.NetworkRandom.Float(0f, 1f) < ulist.UpgradePerComponentChance)
                {
                    int repeatResult = -1;
                    float r1 = s.NetworkRandom.Float(0f, 1f);
                    string sel = BTRandomMechComponentUpgrader_UpgradeList.GetUpgradeFromRandom(l, r1, s.CurrentDate, out bool _, ref repeatResult, out string _, out string _);
                    if (sel != null)
                    {
                        MechComponentDef d = GetComponentDefFromID(s, sel);
                        ChassisLocations loc = ChassisLocations.None;
                        foreach (ChassisLocations lo in Locations)
                        {
                            if (CanUpgrade(null, d, canFreeTonns, mDef, lo, inv) && CanPutComponentIntoLoc(d, lo))
                            {
                                loc = lo;
                                break;
                            }
                        }
                        if (loc == ChassisLocations.None)
                        {
                            BTRandomMechComponentUpgrader_Init.Log.Log($"cannot add {sel}, rolled {r1}");
                            continue;
                        }
                        BTRandomMechComponentUpgrader_Init.Log.Log($"adding {sel} into {loc}, rolled {r1}");
                        MechComponentRef r = new MechComponentRef(sel, null, d.ComponentType, loc, -1, ComponentDamageLevel.Functional, false);
                        r.SetComponentDef(d);
                        inv.Add(r);
                    }
                    else
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log($"found no addition for {l[0].ID}, rolled {r1}");
                        foreach (BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry e in l)
                            BTRandomMechComponentUpgrader_Init.Log.Log($"\t limits {e.ID} {e.RandomLimit}");
                    }
                }
            }
            mDef.SetInventory(inv.ToArray());
        }

        private static void CorrectTonnage(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist)
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
                MechComponentDef d = GetComponentDefFromID(s, id);
                while (tonnage + d.Tonnage <= mDef.Chassis.Tonnage)
                {
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
                    tonnage += d.Tonnage;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"added {r.ComponentDefID} to use free weight");
                }
            }
            mDef.SetInventory(inv.ToArray());

            float armorfact = GetMechArmorPointFactor(mDef);
            BTRandomMechComponentUpgrader_Init.Log.Log($"correcting tonnage 2: armor (each armor point costs {armorfact} t)");
            while (tonnage + armorfact <= mDef.Chassis.Tonnage)
            {
                bool assOne = false;
                foreach (ChassisLocations c in Locations)
                {
                    if (tonnage + armorfact >= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedArmor >= mDef.GetChassisLocationDef(c).MaxArmor)
                        continue;
                    l.AssignedArmor += 1;
                    l.CurrentArmor += 1;
                    tonnage += armorfact;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"increased {l} armor to {l.AssignedArmor}");
                    assOne = true;
                }
                foreach (ChassisLocations c in RearArmoredLocs)
                {
                    if (tonnage + armorfact >= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedRearArmor >= mDef.GetChassisLocationDef(c).MaxRearArmor)
                        continue;
                    l.AssignedRearArmor += 1;
                    l.CurrentRearArmor += 1;
                    tonnage += armorfact;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"increased {l} rear armor to {l.AssignedRearArmor}");
                    assOne = true;
                }
                if (!assOne)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("no free armor location found!");
                    break;
                }
            }
            while (tonnage > mDef.Chassis.Tonnage)
            {
                bool assOne = false;
                foreach (ChassisLocations c in Locations)
                {
                    if (tonnage <= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedArmor <= 1)
                        continue;
                    l.AssignedArmor -= 1;
                    l.CurrentArmor -= 1;
                    tonnage -= armorfact;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"decreased {c} armor to {l.AssignedArmor}");
                    assOne = true;
                }
                foreach (ChassisLocations c in RearArmoredLocs)
                {
                    if (tonnage <= mDef.Chassis.Tonnage)
                        break;
                    LocationLoadoutDef l = mDef.GetLocationLoadoutDef(c);
                    if (l.AssignedRearArmor <= 1)
                        continue;
                    l.AssignedRearArmor -= 1;
                    l.CurrentRearArmor -= 1;
                    tonnage -= armorfact;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"decreased {c} rear armor to {l.AssignedRearArmor}");
                    assOne = true;
                }
                if (!assOne)
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log("no free armor location found!");
                    break;
                }
            }


            BTRandomMechComponentUpgrader_Init.Log.Log($"final weight: {tonnage}/{mDef.Chassis.Tonnage}");
        }

        private static void CheckUpgrades(MechDef mDef, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList ulist, ref float canFreeTonns)
        {
            Dictionary<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[], int> repeatUpgradeResults = new Dictionary<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[], int>();
            foreach (MechComponentRef r in mDef.Inventory)
            {
                if (r.IsFixed)
                    continue;
                CheckForAndPerformUpgrade(r, s, ulist, ref canFreeTonns, mDef, repeatUpgradeResults);
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

        public static void CheckForAndPerformUpgrade(MechComponentRef r, SimGameState s, BTRandomMechComponentUpgrader_UpgradeList l, ref float canFreeTonns, MechDef mech, Dictionary<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[], int> repeatResults)
        {
            BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[] le = l.GetUpgradeArrayAndOffset(r.Def.Description.Id, out float minr);
            if (le != null)
            {
                int repeat = -2;
                if (repeatResults.ContainsKey(le))
                    repeat = repeatResults[le];

                if (repeat<0 && s.NetworkRandom.Float(0f, 1f) > l.UpgradePerComponentChance) // roll if upgrade only if no repeat is saved
                    return;

                float r1 = s.NetworkRandom.Float(minr, 1f);
                string sel = BTRandomMechComponentUpgrader_UpgradeList.GetUpgradeFromRandom(le, r1, s.CurrentDate, out bool ReCheckUpgrade, ref repeat, out string swapAmmoFrom, out string swapAmmoTo);
                if (sel != null)
                {
                    if (repeat <= -2)
                        repeat = -1;
                    repeatResults[le] = repeat;
                    BTRandomMechComponentUpgrader_Init.Log.Log($"changing {r.ComponentDefID} -> {sel}, rolled {r1}");
                    MechComponentDef d = GetComponentDefFromID(s, sel);
                    if (!CanUpgrade(r, d, canFreeTonns, mech, r.MountedLocation, mech.Inventory))
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
                {
                    BTRandomMechComponentUpgrader_Init.Log.Log($"found no upgrade for {r.ComponentDefID}, rolled {r1}");
                    foreach (BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry e in le)
                        BTRandomMechComponentUpgrader_Init.Log.Log($"\t limits {e.ID} {e.RandomLimit}");
                }
            }
            else
            {
                BTRandomMechComponentUpgrader_Init.Log.Log($"upgradesublist null for {r.ComponentDefID}");
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
            r.SetComponentDef(d);
            canFreeTonns -= r.Def.Tonnage;
        }

        public static bool CanUpgrade(MechComponentRef r, MechComponentDef d, float canFreeTonns, MechDef mech, ChassisLocations loc, IEnumerable<MechComponentRef> inv)
        {
            if (d == null)
                return false;
            if (r != null)
            {
                canFreeTonns += r.Def.Tonnage;
            }
            if (canFreeTonns < d.Tonnage)
                return false;
            int slots = GetFreeSlotsInLoc(mech, inv, loc, r);
            if (slots < d.InventorySize)
                return false;
            return true;
        }

        public static int GetFreeSlotsInLoc(MechDef mech, IEnumerable<MechComponentRef> inv, ChassisLocations loc, MechComponentRef except)
        {
            int slots = mech.GetChassisLocationDef(loc).InventorySlots;
            foreach (MechComponentRef i in inv)
                if (i.MountedLocation == loc && i != except)
                    slots -= i.Def.InventorySize;
            return slots;
        }

        public static bool CanPutComponentIntoLoc(MechComponentDef d, ChassisLocations loc)
        {
            return (d.AllowedLocations & loc) != ChassisLocations.None;
        }

        public static float GetMechArmor(MechDef m)
        {
            float r = 0;
            foreach (ChassisLocations l in Locations)
            {
                LocationLoadoutDef v = m.GetLocationLoadoutDef(l);
                r += v.AssignedArmor;
            }
            foreach (ChassisLocations l in RearArmoredLocs)
            {
                LocationLoadoutDef v = m.GetLocationLoadoutDef(l);
                r += v.AssignedRearArmor;
            }
            return r;
        }

        public static float GetMechArmorPointFactor(MechDef m)
        {
            float t = 0;
            float _ = 0;
            MechStatisticsRules.CalculateTonnage(m, ref t, ref _);
            t -= m.Chassis.InitialTonnage;
            foreach (MechComponentRef i in m.Inventory)
                t -= i.Def.Tonnage;
            float arm = GetMechArmor(m);
            return t / arm;
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
