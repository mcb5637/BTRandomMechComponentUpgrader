using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTRandomMechComponentUpgrader
{
    public class AmmoTracker
    {
        public class AmmoGroup
        {
            public List<string> AmmoLockout = new List<string>();
            public Dictionary<string, int> IdealAmmoRatios = new Dictionary<string, int>();
            public Dictionary<string, int> CurrentAmmoRatios = new Dictionary<string, int>();
            public Dictionary<string, int> RemovedAmmoTypes = new Dictionary<string, int>();
            public Dictionary<string, int> AddedAmmoTypes = new Dictionary<string, int>();
            public Dictionary<string, int> IdealBoxes = new Dictionary<string, int>();

            public UpgradeSubList LongestSublist = null;


            internal void CheckLen(UpgradeSubList l)
            {
                if (l.AmmoTypes.Length > (LongestSublist?.AmmoTypes?.Length ?? 0))
                    LongestSublist = l;
            }

            public void CalculateIdealBoxes()
            {
                if (IdealAmmoRatios.Count == 0)
                    return;
                var totalBoxes = CurrentAmmoRatios.Values.Sum();
                double ratioSum = IdealAmmoRatios.Where(kv => !AmmoLockout.Contains(kv.Key)).Select(kv => kv.Value).Sum();
                int assigned = 0;
                foreach (var kv in IdealAmmoRatios.Skip(1))
                {
                    if (AmmoLockout.Contains(kv.Key))
                        continue;
                    int num = (int)Math.Floor(kv.Value / ratioSum * totalBoxes);
                    assigned += num;
                    IdealBoxes[kv.Key] = assigned;
                }
                var f = IdealAmmoRatios.First();
                IdealBoxes[f.Key] = totalBoxes - assigned;
            }

            internal string ToLogString(string groupName)
            {
                return $"{groupName} IdealAmmoRatios:{Join(IdealAmmoRatios)} IdealBoxes:{Join(IdealBoxes)} CurrentAmmoRatios:{Join(CurrentAmmoRatios)}";

                string Join(Dictionary<string, int> d)
                {
                    return string.Join(",", d.Select(kv => $"{kv.Key}:{kv.Value}"));
                }
            }

            internal IEnumerable<string> IterIdealBoxIDs()
            {
                foreach (var kv in IdealBoxes)
                {
                    if (kv.Value <= 0)
                        continue;
                    for (int i = 0; i < kv.Value; i++)
                        yield return kv.Key;
                }
            }
        }

        public Dictionary<string, AmmoGroup> AmmoGroups;

        public AmmoGroup GetGroup(string name)
        {
            if (AmmoGroups.TryGetValue(name, out AmmoGroup group))
            {
                return group;
            }
            else
            {
                AmmoGroup n = new AmmoGroup();
                AmmoGroups.Add(name, n);
                return n;
            }
        }


        public void OnChange(MechComponentDef orig, MechComponentDef chang, UpgradeSubList rootSubList, UpgradeSubList lastSubList)
        {
            if (orig is WeaponDef o)
            {
                AmmoGroup og = GetGroup(rootSubList.AmmoGroup);
                foreach (var a in rootSubList.AmmoTypes)
                    og.RemovedAmmoTypes.AddToDictDefault(a.ID, 1);
            }
            if (chang is WeaponDef c)
            {
                AmmoGroup ng = GetGroup(lastSubList.AmmoGroup);
                foreach (var a in lastSubList.AmmoTypes)
                    ng.AddedAmmoTypes.AddToDictDefault(a.ID, 1);
            }
        }
    }

    public class Modifier_AmmoSwapper : IMechDefSpawnModifier
    {
        public static Action<MechDef, SimGameState, UpgradeList, float, AmmoTracker, MechDef> SmartAmmoAdjust = (m, s, u, f, t, d) => {};

        public void ModifyMech(MechDef mDef, SimGameState s, UpgradeList ulist, ref float canFreeTonns, AmmoTracker changedAmmoTypes, MechDef fromData)
        {
            Main.Log.Log("checking changed ammo types");
            List<MechComponentRef> inv = mDef.Inventory.ToList();


            foreach (var r in inv)
            {
                if (r.Def is WeaponDef w)
                {
                    UpgradeSubList sl = ulist.GetUpgradeSubListAndOffset(w.Description.Id, SubListType.Main, out int _);
                    if (sl == null)
                        continue;
                    AmmoTracker.AmmoGroup g = changedAmmoTypes.GetGroup(sl.AmmoGroup);
                    g.CheckLen(sl);
                    foreach (var e in sl.AmmoTypes)
                    {
                        g.IdealAmmoRatios.AddToDictDefault(e.ID, e.AmmoWeight);
                    }
                    foreach (var a in MechProcessor.AddonHelp.GetAddons(mDef, mDef.Inventory, r))
                    {
                        var e = sl.Addons.Where(x => x.ID == a.ComponentDefID).FirstOrDefault();
                        if (e != null)
                        {
                            foreach (var l in e.AmmoLockoutByAddon)
                            {
                                if (!g.AmmoLockout.Contains(l))
                                    g.AmmoLockout.Add(l);
                            }
                        }
                    }
                }
                else if (r.Def is AmmunitionBoxDef a)
                {
                    UpgradeSubList sl = ulist.GetUpgradeSubListAndOffset(a.Description.Id, SubListType.Ammo, out int _);
                    if (sl == null)
                        continue;
                    AmmoTracker.AmmoGroup g = changedAmmoTypes.GetGroup(sl.AmmoGroup);
                    g.CurrentAmmoRatios.AddToDictDefault(a.Description.Id, 1);
                }
            }

            Main.Log.Log("ammo groups before smart adjust:");
            foreach (var g in changedAmmoTypes.AmmoGroups)
            {
                g.Value.CalculateIdealBoxes();
                Main.Log.Log(g.Value.ToLogString(g.Key));
            }

            SmartAmmoAdjust(mDef, s, ulist, canFreeTonns, changedAmmoTypes, fromData);

            Main.Log.Log("ammo groups after smart adjust:");
            foreach (var g in changedAmmoTypes.AmmoGroups)
            {
                Main.Log.Log(g.Value.ToLogString(g.Key));

                var boxids = g.Value.IterIdealBoxIDs().GetEnumerator();
                var boxes = inv.Where(x => g.Value.CurrentAmmoRatios.ContainsKey(x.ComponentDefID)).ToList();

                while (true)
                {
                    bool hasid = boxids.MoveNext();
                    if (!hasid && boxes.Count == 0)
                        break;
                    var box = boxes.Count == 0 ? null : boxes[0];
                    var newid = hasid ? boxids.Current : null;
                    if (box != null)
                        boxes.RemoveAt(0);

                    if (box == null && newid == null)
                    {
                        Main.Log.Log("changing ammo null -> null (both null ???)");
                    }
                    else if (newid == null)
                    {
                        Main.Log.Log($"changing ammo {box.ComponentDefID} -> null (remove)");
                        inv.Remove(box);
                    }
                    else if (box == null)
                    {
                        Main.Log.Log($"changing ammo null -> {newid} (try add)");
                        TryAddAmmoBox(mDef, ref canFreeTonns, inv, GetBox(newid, s));
                    }
                    else
                    {
                        Main.Log.Log($"changing ammo {box.ComponentDefID} -> {newid} (swap)");
                        var repl = GetBox(newid, s);
                        if (box.CanUpgrade(repl, canFreeTonns, mDef, box.MountedLocation, inv))
                        {
                            Main.Log.Log($"upgrading");
                            box.DoUpgrade(repl, ref canFreeTonns);
                        }
                    }
                }
            }
            mDef.SetInventory(inv.ToArray());
        }

        private static void TryAddAmmoBox(MechDef mDef, ref float canFreeTonns, List<MechComponentRef> inv, AmmunitionBoxDef box)
        {
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

        internal AmmunitionBoxDef GetBox(string id, SimGameState s)
        {
            return s.DataManager.AmmoBoxDefs.Get(id);
        }
    }
}
