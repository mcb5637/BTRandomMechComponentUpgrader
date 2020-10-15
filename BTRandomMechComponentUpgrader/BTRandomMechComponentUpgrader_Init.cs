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

[assembly:AssemblyVersion("1.0.0")]

namespace BTRandomMechComponentUpgrader
{
    class BTRandomMechComponentUpgrader_Init
    {
        public static BTRandomMechComponentUpgrader_Settings Sett;
        public static ILog Log;

        public static void Init(string directory, string settingsJSON)
        {
            Log = Logger.GetLogger("BTRandomMechComponentUpgrader");
            try
            {
                Sett = JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_Settings>(settingsJSON);
                Sett.UpgradeLists = new BTRandomMechComponentUpgrader_UpgradeList[Sett.UpgradeListNames.Length];
                for (int i = 0; i < Sett.UpgradeListNames.Length; i++)
                {
                    Sett.UpgradeLists[i] = LoadUList(Sett.UpgradeListNames[i], directory);
                    LoadListComponents(Sett.UpgradeLists[i], directory);
                    Sett.UpgradeLists[i].CalculateLimits();
                }
            }
            catch (Exception e)
            {
                Sett = new BTRandomMechComponentUpgrader_Settings();
                Log.LogException(e);
            }
            HarmonyInstance harmony = HarmonyInstance.Create("com.github.mcb5637.BTRandomMechComponentUpgrader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void LoadListComponents(BTRandomMechComponentUpgrader_UpgradeList li, string directory)
        {
            if (li.LoadUpgrades.Length > 0)
            {
                if (li.Upgrades == null)
                    li.Upgrades = new BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[li.LoadUpgrades.Length][];
                for (int j = 0; j < li.LoadUpgrades.Length; j++)
                {
                    string name = li.LoadUpgrades[j];
                    if (name != null)
                        li.Upgrades[j] = LoadCList(name, directory);
                }
            }
            if (li.LoadAdditions.Length > 0)
            {
                if (li.Additions == null)
                    li.Additions = new BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[li.LoadAdditions.Length][];
                for (int j = 0; j < li.LoadAdditions.Length; j++)
                {
                    string name = li.LoadAdditions[j];
                    if (name != null)
                        li.Additions[j] = LoadCList(name, directory);
                }
            }
        }

        public static BTRandomMechComponentUpgrader_UpgradeList LoadUList(string name, string dir)
        {
            string path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_UpgradeList>(file);
        }

        public static BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[] LoadCList(string name, string dir)
        {
            string path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_UpgradeList.UpgradeEntry[]>(file);
        }
    }
}
