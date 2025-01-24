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

[assembly:AssemblyVersion("1.3.1")]

namespace BTRandomMechComponentUpgrader
{
    class Main
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
                Dictionary<string, UpgradeSubList> entries = new Dictionary<string, UpgradeSubList>();
                MechProcessor.UpgradeLists = new List<UpgradeList>();
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeSubList"])
                {
                    missing = kv.Value.FilePath;
                    UpgradeSubList sublist = LoadUpgradeSubList(kv.Value.FilePath);
                    sublist.Name = kv.Value.FileName;
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
                    MechProcessor.UpgradeLists.Add(ulist);
                }
                MechProcessor.UpgradeLists.Sort();
                Log.Log($"loaded with {MechProcessor.UpgradeLists.Count()} upgradelists");
                foreach (UpgradeList l in MechProcessor.UpgradeLists)
                    Log.Log($"list: {l.Name} with sort {l.Sort}");
            }
            catch (Exception e)
            {
                MechProcessor.UpgradeLists = new List<UpgradeList>();
                Log.LogException(e);
                Log.LogError(missing);
            }
        }

        private static void LoadListComponents(string[] load, List<UpgradeSubList> data, Dictionary<string, UpgradeSubList> entries, out string missing)
        {
            if (load != null && load.Length > 0)
            {
                foreach (string l in load)
                {
                    if (l != null)
                    {
                        missing = l;
                        string e = l;
                        if (!e.EndsWith(".json"))
                            e += ".json";
                        data.Add(entries[e]);
                    }
                }
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

        public static UpgradeSubList LoadUpgradeSubList(string name, string dir = null)
        {
            string path;
            if (dir == null)
                path = name;
            else
                path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UpgradeSubList>(file);
        }
    }
}
