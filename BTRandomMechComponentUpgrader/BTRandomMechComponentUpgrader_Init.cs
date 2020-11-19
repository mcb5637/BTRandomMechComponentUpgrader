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
            if (!customResources.ContainsKey("ComponentUpgradeList") || !customResources.ContainsKey("ComponentUpgradeSubList"))
            {
                Log.LogError("Error: Missing custom Resource!");
                return;
            }
            string missing = "";
            try
            {
                Dictionary<string, BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]> entries = new Dictionary<string, BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]>();
                UpgradeLists = new List<BTRandomMechComponentUpgrader_UpgradeList>();
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeSubList"])
                {
                    missing = kv.Value.FilePath;
                    entries.Add(kv.Value.FileName, LoadCList(kv.Value.FilePath));
                }
                foreach (KeyValuePair<string, VersionManifestEntry> kv in customResources["ComponentUpgradeList"])
                {
                    missing = kv.Value.FilePath;
                    BTRandomMechComponentUpgrader_UpgradeList ulist = LoadUList(kv.Value.FilePath);
                    ulist.Name = kv.Value.FileName;
                    LoadListComponents(ulist.LoadUpgrades, ulist.Upgrades, entries, out missing);
                    LoadListComponents(ulist.LoadAdditions, ulist.Additions, entries, out missing);
                    //ulist.CalculateLimits();
                    UpgradeLists.Add(ulist);
                }
                UpgradeLists.Sort();
            }
            catch (Exception e)
            {
                UpgradeLists = new List<BTRandomMechComponentUpgrader_UpgradeList>();
                Log.LogException(e);
                Log.LogError(missing);
            }
        }

        private static void LoadListComponents(string[] load, List<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]> data, Dictionary<string, BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]> entries, out string missing)
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
