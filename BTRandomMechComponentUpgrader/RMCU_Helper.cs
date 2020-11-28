using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    static class RMCU_Helper
    {
        public static readonly ChassisLocations[] Locations = new ChassisLocations[] { ChassisLocations.CenterTorso, ChassisLocations.Head,
            ChassisLocations.RightArm, ChassisLocations.RightLeg, ChassisLocations.RightTorso,
            ChassisLocations.LeftArm, ChassisLocations.LeftLeg, ChassisLocations.LeftTorso};
        public static readonly ChassisLocations[] RearArmoredLocs = new ChassisLocations[] { ChassisLocations.CenterTorso, ChassisLocations.RightTorso, ChassisLocations.LeftTorso };

        public static MechComponentDef GetComponentDefFromID(this SimGameState s, string id)
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

        public static void DoUpgrade(this MechComponentRef r, MechComponentDef d, ref float canFreeTonns)
        {
            canFreeTonns += r.Def.Tonnage;
            r.ComponentDefID = d.Description.Id;
            r.SetComponentDef(d);
            canFreeTonns -= r.Def.Tonnage;
        }

        public static bool CanUpgrade(this MechComponentRef r, MechComponentDef d, float canFreeTonns, MechDef mech, ChassisLocations loc, IEnumerable<MechComponentRef> inv)
        {
            if (d == null)
                return false;
            if (r != null)
            {
                canFreeTonns += r.Def.Tonnage;
            }
            if (canFreeTonns < d.Tonnage)
                return false;
            int slots = mech.GetFreeSlotsInLoc(inv, loc, r);
            if (slots < d.InventorySize)
                return false;
            return true;
        }

        public static int GetFreeSlotsInLoc(this MechDef mech, IEnumerable<MechComponentRef> inv, ChassisLocations loc, MechComponentRef except)
        {
            int slots = mech.GetChassisLocationDef(loc).InventorySlots;
            foreach (MechComponentRef i in inv)
                if (i.MountedLocation == loc && i != except)
                    slots -= i.Def.InventorySize;
            return slots;
        }

        public static bool CanPutComponentIntoLoc(this MechComponentDef d, ChassisLocations loc)
        {
            return (d.AllowedLocations & loc) != ChassisLocations.None;
        }

        public static float GetMechArmor(this MechDef m)
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

        public static float GetMechArmorPointFactor(this MechDef m)
        {
            float t = 0;
            float _ = 0;
            MechStatisticsRules.CalculateTonnage(m, ref t, ref _);
            t -= m.Chassis.InitialTonnage;
            foreach (MechComponentRef i in m.Inventory)
                t -= i.Def.Tonnage;
            float arm = m.GetMechArmor();
            return t / arm;
        }

        public static ChassisLocations SearchLocationToAddComponent(this MechDef mDef, MechComponentDef d, float canFreeTonns, IEnumerable<MechComponentRef> inv, MechComponentRef current, ChassisLocations avoidIfPossible)
        {
            foreach (ChassisLocations lo in Locations)
            {
                if (lo != avoidIfPossible && CanUpgrade(current, d, canFreeTonns, mDef, lo, inv) && d.CanPutComponentIntoLoc(lo))
                {
                    return lo;
                }
            }
            if (avoidIfPossible != ChassisLocations.None && CanUpgrade(current, d, canFreeTonns, mDef, avoidIfPossible, inv) && d.CanPutComponentIntoLoc(avoidIfPossible))
            {
                return avoidIfPossible;
            }
            return ChassisLocations.None;
        }
    }
}
