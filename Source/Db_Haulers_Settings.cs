using DB_All_Haulers_Are_Pack_Animals;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace All_Haulers_Are_Pack_Animals
{
    /*
     * Standard mod settings stuff here 
     */
    public class Db_Haulers_Settings : ModSettings
    {
        public bool requirePackAnimalProp;

        public Dictionary<string, bool> trainableNames = new Dictionary<string, bool>();
        public List<string> trainables = new List<string>();
        public List<bool> valid = new List<bool>();
        public Dictionary<TrainableDef, bool> cachedTrainables = new Dictionary<TrainableDef, bool>();
        public Dictionary<TrainableDef, bool> LoadedTrainables
        {
            get
            {
                if (cachedTrainables.NullOrEmpty())
                {
                    for (int i = 0; i < trainableNames.Count; i++)
                    {

                        TrainableDef namedSilentFail = DefDatabase<TrainableDef>.GetNamedSilentFail(trainableNames.ElementAt(i).Key);
                        if (namedSilentFail != null)
                        {
                            cachedTrainables.Add(namedSilentFail, trainableNames.ElementAt(i).Value);
                        }
                    }
                }
                return cachedTrainables;
            }
        }

        //IEnumerable with only the trainables selected in the mod settings
        public IEnumerable<TrainableDef> requiredTrainables => from td in LoadedTrainables where LoadedTrainables[td.Key] select td.Key;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref requirePackAnimalProp, "requirePackAnimalProp", true);
            Scribe_Collections.Look(ref trainableNames, "trainableNames");
        }
    }

    [StaticConstructorOnStartup]
    public static class Db_Haulers_Startup
    {
        public static void CheckTrainables()
        {
            //Log.Message((Db_AllHaulersArePackAnimals.settings != null).ToString());
            Db_Haulers_Settings settings = Db_AllHaulersArePackAnimals.settings;
            if (settings.trainableNames.NullOrEmpty())
            {
                settings.trainableNames = new Dictionary<string, bool>();
            }
            foreach (TrainableDef item in DefDatabase<TrainableDef>.AllDefs.ToList())
            {
                if (item != null && !settings.trainableNames.ContainsKey(item.defName))
                {
                    if (item.defName == "Haul")
                    {
                        settings.trainableNames.Add(item.defName, true);
                        settings.trainables.Add(item.defName);
                        settings.valid.Add(true);
                    }
                    else
                    {
                        settings.trainableNames.Add(item.defName, false);
                        settings.trainables.Add(item.defName);
                        settings.valid.Add(false);
                    }
                }
            }
        }

        static Db_Haulers_Startup()
        {
            CheckTrainables();
        }

    }
}