using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTRandomMechComponentUpgrader
{
    public class AddonHelper
    {
        public virtual bool AddonsEnabled => false;

        public virtual IEnumerable<MechComponentRef> GetAddons(MechDef m, IEnumerable<MechComponentRef> inventory, MechComponentRef r)
        {
            return Array.Empty<MechComponentRef>();
        }

        public virtual void SetAddonTarget(MechComponentRef c, MechComponentRef tar)
        {

        }
    }
}
