using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class Modifier_Upgrades : IMechDefSpawnModifier
    {
        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, List<string[]> changedAmmoTypes, MechDef fromData)
        {
            Main.Log.Log("checking upgrade sublists");
            foreach (MechComponentRef r in mDef.Inventory)
            {
                if (r.IsFixed)
                    continue;
                CheckForAndPerformUpgrade(r, s, ulist, ref canFreeTonns, mDef, changedAmmoTypes);
            }
        }

        private void CheckForAndPerformUpgrade(MechComponentRef r, SimGameState s, UpgradeList l, ref float canFreeTonns, MechDef mech, List<string[]> changedAmmoTypes)
        {
            string baseid = r.Def.Description.Id;
            if (s.NetworkRandom.Float(0f, 1f) > l.UpgradePerComponentChance)
                return;

            string log = baseid;
            UpgradeEntry ue = l.RollEntryFromMatchingSubList(baseid, s.NetworkRandom, s.CurrentDate, SubListType.Main, ref log, l.UpgradePerComponentChance);
            if (ue != null)
            {
                MechComponentDef d = s.GetComponentDefFromID(ue.ID);
                if (r.CanUpgrade(d, canFreeTonns, mech, r.MountedLocation, mech.Inventory))
                {
                    CheckChangedAmmo(r.Def, d, changedAmmoTypes);
                    r.DoUpgrade(d, ref canFreeTonns);
                    Main.Log.Log("changing " + log);
                }
                else
                    Main.Log.Log("cannot upgrade " + log);
            }
            else
                Main.Log.Log("upgrade unavailable " + log);
        }

        private void CheckChangedAmmo(MechComponentDef orig, MechComponentDef chang, List<string[]> changedAmmoTypes)
        {
            WeaponDef o = orig as WeaponDef;
            WeaponDef c = chang as WeaponDef;
            if (o == null || c == null)
                return;
            if (string.IsNullOrEmpty(o.AmmoCategoryToAmmoId) || string.IsNullOrEmpty(c.AmmoCategoryToAmmoId))
                return;
            if (o.AmmoCategoryToAmmoId.Equals(c.AmmoCategoryToAmmoId))
                return;
            if (changedAmmoTypes.Where((a) => a[0].Equals(o.AmmoCategoryToAmmoId) && a[1].Equals(c.AmmoCategoryToAmmoId)).Any())
                return;
            changedAmmoTypes.Add(new string[] { o.AmmoCategoryToAmmoId, c.AmmoCategoryToAmmoId });
        }
    }
}
