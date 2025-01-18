using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTRandomMechComponentUpgrader
{
    class Modifier_AmmoSwapper : IMechDefSpawnModifier // TODO check for CAC
    {
        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, List<string[]> changedAmmoTypes, MechDef fromData)
        {
            Main.Log.Log("checking changed ammo types");
            List<MechComponentRef> inv = mDef.Inventory.ToList();
            foreach (string[] ca in changedAmmoTypes)
            {
                AmmunitionBoxDef basebox = GetMainAmmoBox(ca[0], s);
                AmmunitionBoxDef box = GetMainAmmoBox(ca[1], s);
                if (box == null && basebox == null)
                {
                    Main.Log.Log($"changing ammo {ca[0]} -> {ca[1]} (both null ???)");
                    continue;
                }
                if (box == null) // removed ammo dependency, remove all ammoboxes as well, tonnagefixer will take care of missing tonnage
                {
                    Main.Log.Log($"changing ammo {ca[0]} -> {ca[1]} (box null, removing all)");
                    inv.RemoveAll((a) => ca[0].Equals((a.Def as AmmunitionBoxDef)?.AmmoID));
                    continue;
                }
                if (basebox == null) // added ammo dependency, try to add an ammobox
                {
                    TryAddAmmoBox(mDef, ref canFreeTonns, inv, ca, box);
                    continue;
                }
                int numOldBox = CountAmmoBoxes(inv, ca[0]);
                float or = (float)GetAmmoTypeUsagePerTurn(inv, ca[0]) / basebox.Capacity;
                float ne = (float)GetAmmoTypeUsagePerTurn(inv, ca[1]) / box.Capacity;
                if (numOldBox <= 0 || (numOldBox==1 && or>0)) // should not happen, but just in case
                {
                    TryAddAmmoBox(mDef, ref canFreeTonns, inv, ca, box);
                    continue;
                }
                float ratio = ne / (or + ne);
                int swap = Mathf.RoundToInt(numOldBox * ratio);
                Main.Log.Log($"changing ammo {ca[0]} -> {ca[1]} (usage {or}/{ne}, changing {swap} boxes)");
                foreach (MechComponentRef r in inv.Where((a) => ca[0].Equals((a.Def as AmmunitionBoxDef)?.AmmoID))) {
                    if (swap <= 0)
                        break;
                    if (r.CanUpgrade(box, canFreeTonns, mDef, r.MountedLocation, inv))
                    {
                        Main.Log.Log($"changing ammo {r.Def.Description.Id} -> {box.Description.Id}");
                        r.DoUpgrade(box, ref canFreeTonns);
                        swap--;
                    }
                    else
                        Main.Log.Log($"cannot change ammo {r.Def.Description.Id} -> {box.Description.Id}");
                }
                if (swap > 0)
                    Main.Log.Log($"missed {swap} changes");
            }
            mDef.SetInventory(inv.ToArray());
        }

        private static void TryAddAmmoBox(MechDef mDef, ref float canFreeTonns, List<MechComponentRef> inv, string[] ca, AmmunitionBoxDef box)
        {
            Main.Log.Log($"changing ammo {ca[0]} -> {ca[1]} (basebox null, try adding one)");
            ChassisLocations loc = mDef.SearchLocationToAddComponent(box, canFreeTonns, inv, null, ChassisLocations.None);
            if (loc != ChassisLocations.None)
            {
                Main.Log.Log($"adding into {loc}");
                MechComponentRef r = new MechComponentRef(box.Description.Id, null, box.ComponentType, loc, -1, ComponentDamageLevel.Functional, false);
                r.SetComponentDef(box);
                inv.Add(r);
                canFreeTonns -= box.Tonnage;
            }
        }

        public int GetAmmoTypeUsagePerTurn(IEnumerable<MechComponentRef> inv, string ammo)
        {
            int r = 0;
            foreach (WeaponDef w in inv.Select((a) => a.Def).OfType<WeaponDef>().Where((a) => ammo.Equals(a.AmmoCategoryToAmmoId))) {
                r += w.ShotsWhenFired;
            }
            return r;
        }

        public AmmunitionBoxDef GetMainAmmoBox(string ammo, SimGameState s)
        {
            return s.DataManager.AmmoBoxDefs.Where((a) => ammo.Equals(a.Value.AmmoID)).First().Value;
        }

        public int CountAmmoBoxes(IEnumerable<MechComponentRef> inv, string ammo)
        {
            return inv.Select((a) => a.Def).OfType<AmmunitionBoxDef>().Where((a) => ammo.Equals(a.AmmoID)).Count();
        }
    }
}
