using BattleTech;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTRandomMechComponentUpgrader
{
    public class MechProcessor
    {
        public static IMechDefSpawnModifier[] DefaultModifiers = new IMechDefSpawnModifier[] { new Modifier_Upgrades(), new Modifier_Addon(), new Modifier_Additions(), new Modifier_AmmoSwapper(), new Modifier_TonnageFixInventory(), new Modifier_TonnageFixArmor() };
        public static List<UpgradeList> UpgradeLists;
        public static AddonHelper AddonHelp = new AddonHelper();

        public static MechDef ProcessMech(MechDef mDef, SimGameState s, UpgradeList ulist, IEnumerable<IMechDefSpawnModifier> modifiers = null)
        {
            if (modifiers == null)
                modifiers = DefaultModifiers;

            Main.Log.Log($"upgrading {mDef.Description.Name} {mDef.Chassis.VariantName}, using UpgradeList {ulist.Name}");

            MechDef n = new MechDef(mDef); // dont break original mechdef

            float canFreeTonns = Mathf.Floor(n.Inventory.Sum((r) => ulist.CanRemove.Contains(r.ComponentDefID) ? r.Def.Tonnage : 0f) * ulist.RemoveMaxFactor);

            AmmoTracker changedAmmoTypes = new AmmoTracker();

            foreach (IMechDefSpawnModifier mod in modifiers)
                mod.ModifyMech(n, s, ulist, ref canFreeTonns, changedAmmoTypes, mDef);

            Main.Log.Log("all modifications done");

            return n;
        }

        public static Func<FactionValue, UpgradeList> GetUpgradeList = GetUpgradeListDefault;

        public static UpgradeList GetUpgradeListDefault(FactionValue team)
        {
            foreach (UpgradeList l in UpgradeLists)
            {
                if (l.DoesApplyToFaction(team.ToString()))
                {
                    Main.Log.Log($"searching UpgradeList for faction {team}, found {l.Name}");
                    return l;
                }
            }
            Main.Log.Log($"searching UpgradeList for faction {team}, found none");
            return null;
        }

        public static bool ValidateMech(MechDef m, SimGameState s)
        {
            bool r = true;
            foreach (KeyValuePair<MechValidationType, List<Text>> kv in MechValidationRules.ValidateMechDef(MechValidationLevel.Full, s.DataManager, m, null))
            {
                foreach (Text t in kv.Value)
                {
                    Main.Log.Log($"validation error: {t}");
                    r = false; // dont return here, to log all errors
                }
            }
            return r;
        }
    }
}
