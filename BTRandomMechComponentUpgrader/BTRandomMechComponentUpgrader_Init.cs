using BattleTech;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    class BTRandomMechComponentUpgrader_Init
    {
        public static BTRandomMechComponentUpgrader_Settings Sett;

        public static void Init(string directory, string settingsJSON)
        {
            try
            {
                Sett = JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_Settings>(settingsJSON);
                Sett.UpgradeLists = new BTRandomMechComponentUpgrader_UpgradeList[Sett.UpgradeListNames.Length];
                for (int i = 0; i < Sett.UpgradeListNames.Length; i++)
                {
                    Sett.UpgradeLists[i] = LoadUList(Sett.UpgradeListNames[i], directory);
                    Sett.UpgradeLists[i].CalculateLimits();
                }
            }
            catch (Exception e)
            {
                Sett = new BTRandomMechComponentUpgrader_Settings();
                FileLog.Log(e.Message);
                FileLog.Log(e.StackTrace);
            }
            var harmony = HarmonyInstance.Create("com.github.mcb5637.BTRandomMechComponentUpgrader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static BTRandomMechComponentUpgrader_UpgradeList LoadUList(string name, string dir)
        {
            string path = Path.Combine(dir, name);
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<BTRandomMechComponentUpgrader_UpgradeList>(file);
        }
    }
}
