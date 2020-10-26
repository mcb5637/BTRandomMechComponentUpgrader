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

[assembly:AssemblyVersion("1.1.0")]

namespace BTRandomMechComponentUpgrader
{
    class BTRandomMechComponentUpgrader_Init
    {
        public static BTRandomMechComponentUpgrader_Settings Sett;
        public static ILog Log;

        public static List<BTRandomMechComponentUpgrader_UpgradeList> UpgradeLists;

        public static void Init(string directory, string settingsJSON)
        {
            Log = Logger.GetLogger("BTRandomMechComponentUpgrader");
            try
            {
                Sett = JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_Settings>(settingsJSON);
            }
            catch (Exception e)
            {
                Sett = new BTRandomMechComponentUpgrader_Settings();
                Log.LogException(e);
            }
            if (Sett.LogLevelLog)
                Logger.SetLoggerLevel("BTRandomMechComponentUpgrader", LogLevel.Log);
            HarmonyInstance harmony = HarmonyInstance.Create("com.github.mcb5637.BTRandomMechComponentUpgrader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void FinishedLoading(Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources)
        {
            if (!customResources.ContainsKey("ComponentUpgradeList") || !customResources.ContainsKey("ComponentUpgradeListEntry"))
            {
                Log.LogError("Error: Missing custom Resource!");
                return;
            }
            try
            {
                Dictionary<string, BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]> entries = new Dictionary<string, BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]>();
                UpgradeLists = new List<BTRandomMechComponentUpgrader_UpgradeList>();
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeListEntry"])
                {
                    entries.Add(kv.Value.FileName, LoadCList(kv.Value.FilePath));
                }
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeList"])
                {
                    BTRandomMechComponentUpgrader_UpgradeList ulist = LoadUList(kv.Value.FilePath);
                    ulist.Name = kv.Value.FileName;
                    LoadListComponents(ulist.LoadUpgrades, ulist.Upgrades, entries);
                    LoadListComponents(ulist.LoadAdditions, ulist.Additions, entries);
                    //ulist.CalculateLimits();
                    UpgradeLists.Add(ulist);
                }
                UpgradeLists.Sort();
            }
            catch (Exception e)
            {
                UpgradeLists = new List<BTRandomMechComponentUpgrader_UpgradeList>();
                Log.LogException(e);
            }
        }

        private static void LoadListComponents(string[] load, List<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]> data, Dictionary<string, BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]> entries)
        {
            if (load != null && load.Length > 0)
                foreach (string l in load)
                    if (l != null)
                        data.Add(entries[l]);
        }

        public static BTRandomMechComponentUpgrader_UpgradeList LoadUList(string name, string dir = null)
        {
            string path;
            if (dir == null)
                path = name;
            else
                path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_UpgradeList>(file);
        }

        public static BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[] LoadCList(string name, string dir = null)
        {
            string path;
            if (dir == null)
                path = name;
            else
                path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]>(file);
        }
    }
}
