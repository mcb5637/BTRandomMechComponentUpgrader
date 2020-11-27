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
            BTRandomMechComponentUpgrader_Init.Log.Log("checking changed ammo types");
            foreach (string[] ca in changedAmmoTypes)
            {
                AmmunitionBoxDef basebox = GetMainAmmoBox(ca[0], s);
                AmmunitionBoxDef box = GetMainAmmoBox(ca[1], s);
                if (basebox == null || box == null)
                    continue;
                float or = (float)GetAmmoTypeUsagePerTurn(mDef, ca[0]) / basebox.Capacity;
                float ne = (float)GetAmmoTypeUsagePerTurn(mDef, ca[1]) / box.Capacity;
                float ratio = ne / (or + ne);
                int swap = Mathf.RoundToInt(CountAmmoBoxes(mDef, ca[0]) * ratio);
                BTRandomMechComponentUpgrader_Init.Log.Log($"changing ammo {ca[0]} -> {ca[1]} (usage {or}/{ne}, changing {swap} boxes)");
                foreach (MechComponentRef r in mDef.Inventory.Where((a) => ca[0].Equals((a.Def as AmmunitionBoxDef)?.AmmoID))) {
                    if (swap <= 0)
                        break;
                    if (r.CanUpgrade(box, canFreeTonns, mDef, r.MountedLocation, mDef.Inventory))
                    {
                        BTRandomMechComponentUpgrader_Init.Log.Log($"changing ammo {r.Def.Description.Id} -> {box.Description.Id}");
                        r.DoUpgrade(box, ref canFreeTonns);
                        swap--;
                    }
                    else
                        BTRandomMechComponentUpgrader_Init.Log.Log($"cannot change ammo {r.Def.Description.Id} -> {box.Description.Id}");
                }
                if (swap > 0)
                    BTRandomMechComponentUpgrader_Init.Log.Log($"missed {swap} changes");
            }
        }

        public int GetAmmoTypeUsagePerTurn(MechDef m, string ammo)
        {
            int r = 0;
            foreach (WeaponDef w in m.Inventory.Select((a) => a.Def).OfType<WeaponDef>().Where((a) => ammo.Equals(a.AmmoCategoryToAmmoId))) {
                r += w.ShotsWhenFired;
            }
            return r;
        }

        public AmmunitionBoxDef GetMainAmmoBox(string ammo, SimGameState s)
        {
            return s.DataManager.AmmoBoxDefs.Where((a) => ammo.Equals(a.Value.AmmoID)).First().Value;
        }

        public int CountAmmoBoxes(MechDef m, string ammo)
        {
            return m.Inventory.Select((a) => a.Def).OfType<AmmunitionBoxDef>().Where((a) => ammo.Equals(a.AmmoID)).Count();
        }
    }
}
