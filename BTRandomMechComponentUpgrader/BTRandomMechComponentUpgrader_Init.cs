using BattleTech;
using Harmony;
using HBS.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly:AssemblyVersion("1.3.0")]

namespace BTRandomMechComponentUpgrader
{
    class BTRandomMechComponentUpgrader_Init
    {
        public static Settings Sett;
        public static ILog Log;

        public static void Init(string directory, string settingsJSON)
        {
            Log = Logger.GetLogger("BTRandomMechComponentUpgrader");
            try
            {
                Sett = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception e)
            {
                Sett = new Settings();
                Log.LogException(e);
            }
            if (Sett.LogLevelLog)
                Logger.SetLoggerLevel("BTRandomMechComponentUpgrader", LogLevel.Log);
            HarmonyInstance harmony = HarmonyInstance.Create("com.github.mcb5637.BTRandomMechComponentUpgrader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void FinishedLoading(Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources)
        {
            if (!customResources.ContainsKey("ComponentUpgradeList") || !customResources.ContainsKey("ComponentUpgradeSubList"))
            {
                Log.LogError("Error: Missing custom Resource!");
                return;
            }
            string missing = "";
            try
            {
                Dictionary<string, UpgradeList.UpgradeEntry[]> entries = new Dictionary<string, UpgradeList.UpgradeEntry[]>();
                MechProcesser.UpgradeLists = new List<UpgradeList>();
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeSubList"])
                {
                    missing = kv.Value.FilePath;
                    UpgradeList.UpgradeEntry[] sublist = LoadUpgradeSubList(kv.Value.FilePath);
                    if (sublist.Length > 0)
                        sublist[0].Name = kv.Value.FileName;
                    entries.Add(kv.Value.FileName, sublist);
                }
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeList"])
                {
                    missing = kv.Value.FilePath;
                    UpgradeList ulist = LoadUpgradeList(kv.Value.FilePath);
                    ulist.Name = kv.Value.FileName;
                    LoadListComponents(ulist.LoadUpgrades, ulist.Upgrades, entries, out missing);
                    LoadListComponents(ulist.LoadAdditions, ulist.Additions, entries, out missing);
                    //ulist.CalculateLimits();
                    MechProcesser.UpgradeLists.Add(ulist);
                }
                MechProcesser.UpgradeLists.Sort();
                Log.Log($"loaded with {MechProcesser.UpgradeLists.Count()} upgradelists");
                foreach (UpgradeList l in MechProcesser.UpgradeLists)
                    Log.Log($"list: {l.Name} with sort {l.Sort}");
            }
            catch (Exception e)
            {
                MechProcesser.UpgradeLists = new List<UpgradeList>();
                Log.LogException(e);
                Log.LogError(missing);
            }
        }

        private static void LoadListComponents(string[] load, List<UpgradeList.UpgradeEntry[]> data, Dictionary<string, UpgradeList.UpgradeEntry[]> entries, out string missing)
        {
            if (load != null && load.Length > 0)
                foreach (string l in load)
                    if (l != null)
                    {
                        missing = l;
                        data.Add(entries[l]);
                    }
            missing = "";
        }

        public static UpgradeList LoadUpgradeList(string name, string dir = null)
        {
            string path;
            if (dir == null)
                path = name;
            else
                path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UpgradeList>(file);
        }

        public static UpgradeList.UpgradeEntry[] LoadUpgradeSubList(string name, string dir = null)
        {
            string path;
            if (dir == null)
                path = name;
            else
                path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UpgradeList.UpgradeEntry[]>(file);
        }
    }
}
