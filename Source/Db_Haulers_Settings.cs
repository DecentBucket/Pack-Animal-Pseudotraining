using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace All_Haulers_Are_Pack_Animals
{
    /*
     * Standard mod settings stuff here 
     */
    internal class Db_Haulers_Settings : ModSettings
    {
        public bool requirePackAnimalProp;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref requirePackAnimalProp, "requirePackAnimalProp", true);
        }
    }
}
