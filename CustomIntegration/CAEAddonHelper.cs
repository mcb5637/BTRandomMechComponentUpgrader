using BattleTech;
using BTRandomMechComponentUpgrader;
using CustomActivatableEquipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomIntegration
{
    public class CAEAddonHelper : AddonHelper
    {
        public override bool AddonsEnabled => true;

        public override IEnumerable<MechComponentRef> GetAddons(MechDef m, IEnumerable<MechComponentRef> inventory, MechComponentRef r)
        {
            string localg = r.LocalGUID();
            if (string.IsNullOrEmpty(localg))
                return Enumerable.Empty<MechComponentRef>();
            return inventory.Where(x => x.TargetComponentGUID() == localg);
        }

        public override void SetAddonTarget(MechComponentRef c, MechComponentRef tar)
        {
            string localg = c.LocalGUID();
            if (string.IsNullOrEmpty(localg))
            {
                localg = Guid.NewGuid().ToString();
                c.LocalGUID(localg);
            }
            tar.TargetComponentGUID(localg);
        }
    }
}
