using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public class Modifier_Addon : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, AmmoTracker changedAmmoTypes, MechDef fromData, FactionValue team)
        {
            if (!MechProcessor.AddonHelp.AddonsEnabled)
                return;
            Main.Log.Log("checking addons sublists");
            foreach (MechComponentRef r in mDef.Inventory)
            {
                if (r.IsFixed)
                    continue;
                ModifyAddonsOf(r, s, ulist, ref canFreeTonns, mDef);
            }
        }

        private void ModifyAddonsOf(MechComponentRef r, SimGameState s, UpgradeList l, ref float canFreeTonns, MechDef mech)
        {
            string baseid = r.Def.Description.Id;
            if (s.NetworkRandom.Float(0f, 1f) > l.UpgradePerComponentChance)
                return;

            UpgradeSubList sl = l.GetUpgradeSubListAndOffset(baseid, SubListType.Main, out int _);
            if (sl == null)
            {
                Main.Log.Log($"addon unavailable {baseid} no sublist found");
                return;
            }
            if (sl.Addons.Length == 0)
            {
                Main.Log.Log($"addon unavailable {baseid} no addons in sublist");
                return;
            }

            uint numadd = 0;
            foreach (MechComponentRef ad in MechProcessor.AddonHelp.GetAddons(mech, mech.Inventory, r))
            {
                numadd++;
                canFreeTonns = UpgradeAddon(r, s, l, canFreeTonns, mech, baseid);
            }

            if (numadd == 0)
            {
                AddAddon(r, s, l, ref canFreeTonns, mech, sl);
            }
        }

        private static void AddAddon(MechComponentRef c, SimGameState s, UpgradeList l, ref float canFreeTonns, MechDef mech, UpgradeSubList sl)
        {
            string log = "";
            UpgradeEntry ue = l.RollEntryFromSubList(sl, s.NetworkRandom, -1, s.CurrentDate, SubListType.Addon, ref log, l.UpgradePerComponentChance, out UpgradeSubList _);
            if (ue != null && ue.ID != "")
            {
                MechComponentDef d = s.GetComponentDefFromID(ue.ID);
                ChassisLocations loc = c.MountedLocation;
                if (RMCU_Helper.CanUpgrade(null, d, canFreeTonns, mech, loc, mech.Inventory) && d.CanPutComponentIntoLoc(loc))
                {
                    Main.Log.Log($"cannot add {log} (on {c.ComponentDefID})");
                    return;
                }
                Main.Log.Log($"adding {log} into {loc} (on {c.ComponentDefID})");
                MechComponentRef r = new MechComponentRef(ue.ID, null, d.ComponentType, loc, -1, ComponentDamageLevel.Functional, false);
                r.SetComponentDef(d);
                List<MechComponentRef> inv = mech.Inventory.ToList();
                inv.Add(r);
                mech.SetInventory(inv.ToArray());
                MechProcessor.AddonHelp.SetAddonTarget(r, c);
                canFreeTonns -= d.Tonnage;
            }
            else
                Main.Log.Log($"cannot add, nothing rolled {log} (on {c.ComponentDefID})");
        }

        private static float UpgradeAddon(MechComponentRef r, SimGameState s, UpgradeList l, float canFreeTonns, MechDef mech, string baseid)
        {
            string log = baseid;
            UpgradeEntry ue = l.RollEntryFromMatchingSubList(baseid, s.NetworkRandom, s.CurrentDate, SubListType.Addon, ref log, l.UpgradePerComponentChance, out UpgradeSubList _, out UpgradeSubList _);
            if (ue != null)
            {
                MechComponentDef d = s.GetComponentDefFromID(ue.ID);
                if (r.CanUpgrade(d, canFreeTonns, mech, r.MountedLocation, mech.Inventory))
                {
                    r.DoUpgrade(d, ref canFreeTonns);
                    Main.Log.Log($"changing {log} (on {baseid})");
                }
                else
                    Main.Log.Log($"cannot upgrade {log} (on {baseid})");
            }
            else
                Main.Log.Log($"upgrade unavailable {log} (on {baseid})");
            return canFreeTonns;
        }
    }
}
