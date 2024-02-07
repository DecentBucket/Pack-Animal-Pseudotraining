using All_Haulers_Are_Pack_Animals;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace DB_All_Haulers_Are_Pack_Animals
{
    /* 
     * All the important stuff happens here.
     * 
     * Notes:
     * If a trained animal loses the skill due to decay while forming a caravan, formation procceds regardless, giving items to other pawns capable of carrying items, even exceeding their carrying capacity, however the caravan will become immobile as soon as it departs. 
     * Not a problem if you are sending a caravan from the colony, but if this happens while forming a new caravan from a temporary location, the player will have to abandon some items.
     * This appears to be vanilla behavior, as the same thing will happen if a pack animal dies during formation.
     * 
     * As of the current version (1.4.3901) animals will not lose training steps or skills while in a caravan on the world map, this is likely intended as the vanilla game has no way of training animals in a caravan
     * The Vanilla game picks pack animals for their trade caravans based on a list that is defined by the faction def and biome def, so traders by default ignore animal training altoghether
     */

    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public class Db_AllHaulersArePackAnimals : Mod
    {

        public static Db_Haulers_Settings settings;

        public Db_AllHaulersArePackAnimals(ModContentPack content) : base(content)
        {
            settings = GetSettings<Db_Haulers_Settings>();
            new Harmony("DecentBucket.AllHaulersArePackAnimals.patch").PatchAll();
        }

        public override string SettingsCategory()
        {
            return "AllHaulersCanBePackAnimalsTitle".Translate();
        }

        //TODO(?): Recursive check setting function so the training selection works like the in-game training tab

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.GapLine();
            listing_Standard.CheckboxLabeled("RequirePackAnimalPropName".Translate(), ref settings.requirePackAnimalProp, "RequirePackAnimalPropToolTip".Translate());
            listing_Standard.Gap(35f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            listing_Standard.Label("RequiredTrainingsSectionTitle".Translate());
            Text.Font = GameFont.Small;
            listing_Standard.GapLine();

            listing_Standard.Label("RequiredTrainingsSectionDesc".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            listing_Standard.Gap(5f);
            for (int i = 0; i < settings.LoadedTrainables.Count; i++)
            {
                TrainableDef key = TrainableUtility.TrainableDefsInListOrder[i];
                if (key.defName != "Tameness")
                {
                    bool value = settings.LoadedTrainables[key];
                    listing_Standard.CheckboxLabeled((string)key.LabelCap, ref value, "RequiredTrainingsSectionToolTip".Translate(key.LabelCap));
                    settings.trainableNames[key.defName] = value;
                    settings.LoadedTrainables[key] = value;
                }
            }
            listing_Standard.End();
            base.DoSettingsWindowContents(inRect);
        }

        //----START OF HARMONY PATCH CODE----\\

        static bool ShouldCountAsPackAnimal(Pawn p)
        {
            //Get the mod settings
            bool packAnimalPropRequired = LoadedModManager.GetMod<Db_AllHaulersArePackAnimals>().GetSettings<Db_Haulers_Settings>().requirePackAnimalProp;
            IEnumerable<TrainableDef> requiredTrainables = LoadedModManager.GetMod<Db_AllHaulersArePackAnimals>().GetSettings<Db_Haulers_Settings>().requiredTrainables;

            if (p.training != null)
            {
                //can the pawn even be trained with the required trainables?
                foreach (TrainableDef td in requiredTrainables)
                {
                    if (!p.training.CanAssignToTrain(td))
                    {
                        return p.RaceProps.packAnimal;
                    }
                }
                //at this point in the code, the pawn actually matters to the mod, do the thing!

                //does the pawn have ALL the required training?
                foreach (TrainableDef td in requiredTrainables)
                {
                    if (!p.training.HasLearned(td))
                    {
                        //pawn is missing one (or more) required trainables
                        return false;
                    }
                }
                //pawn has training, can learn, and has learned all required trainables, just one last thing to check
                return packAnimalPropRequired ? p.RaceProps.packAnimal : true;
            }
            else
            {
                return p.RaceProps.packAnimal;
            }

            /*
            for (int i = 0; i < loadedTrainables.Count(); i++)
            {
                if (loadedTrainables.ElementAt(i).Value)
                { 
                    if (p.training != null)
                    {

                    }
                }
            }

            /*
            //check if the pawn can be trained for hauling
            if(p.training != null && p.training.CanAssignToTrain(DefDatabase<TrainableDef>.GetNamed("Haul")))
            {
                if (p.training.HasLearned(DefDatabase<TrainableDef>.GetNamed("Haul")))
                {
                    return packAnimalPropRequired ? p.RaceProps.packAnimal : true;
                }
                return false;
            }
            else
            {
                return p.RaceProps.packAnimal;
            }
            */
        }

        /* Leftover from my attempt at trying to fix the changing capacity problem from the game (read comment at the top of the file) might reuse as a seperate mod
        static void CheckCaravanAndMassCapacity(TrainableDef trainableDef, Pawn p)
        {
            //Log.Message(trainableDef.ToString() + " " + p.ToString() + " " + p.IsFormingCaravan().ToStringYesNo());
            //was the decayed skill haul, and was the skill forgotten?
            if(trainableDef == DefDatabase<TrainableDef>.GetNamed("Haul") && !p.training.HasLearned(DefDatabase<TrainableDef>.GetNamed("Haul")))
            {
                //is this animal forming a caravan?
                if(p.IsFormingCaravan())
                {
                    //check if the caravan can still carry everything
                    p.GetCaravan().RecacheImmobilizedNow();
                    if (p.GetCaravan().ImmobilizedByMass)
                    {
                        //stop the caravan forming, it will be immobile as soon as it leaves
                        CaravanFormingUtility.StopFormingCaravan(p.GetLord());
                    }
                }
            }
        }*/

        //First and most important part: this function determines whether or not a pawn can contribute their carrying capacity to the caravan (and probably some other things, but those don't matter in this context
        [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything))]
        [HarmonyPostfix]
        static void CanEverCarryAnything(ref bool __result, Pawn p)
        {
            if (p.RaceProps.Animal)
            {
                __result = ShouldCountAsPackAnimal(p);
            }
        }

        //this patch prevents pawns from loading items onto animals that aren't marked to carry anything
        [HarmonyPatch(typeof(JobDriver_PrepareCaravan_GatherItems), nameof(JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier))]
        [HarmonyPostfix]
        static void IsUsableCarrier(ref bool __result, ref Pawn p)
        {
            if (p.RaceProps.Animal)
            {
                __result = ShouldCountAsPackAnimal(p) && !MassUtility.IsOverEncumbered(p);
            }
        }

        //Patch for the case where an animal loses haul training while forming a caravan
        /* Also leftover from my attempts to fix the changing capacity problem from the game (again, read the comment at the top of the file)
        [HarmonyPatch(typeof(Pawn_TrainingTracker), nameof(Pawn_TrainingTracker.TrainingTrackerTickRare))]
        [HarmonyTranspiler]
        [HarmonyDebug]
        static IEnumerable<CodeInstruction> TrainspilerTrainingTrackerTickRare(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            //yield return statements would probably look cleaner, HOWEVER
            var code = new List<CodeInstruction>(instructions);
            CodeInstruction pawnCall = null;

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Is(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_TrainingTracker), nameof(Pawn_TrainingTracker.pawn))))
                {
                    pawnCall = code[i];
                    break;
                }
            }

            if (pawnCall != null)
            {
                var insertedInstructions = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    pawnCall,
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Db_AllHaulersArePackAnimals), nameof(CheckCaravanAndMassCapacity)))
                };
                code.InsertRange(code.Count - 1, insertedInstructions);
            }
            return code.AsEnumerable();
        }*/

        //This goes over the caravan tab and recalculates how many animals are pack animals in the caravan tab of the pawns forming the caravan
        //my first transpiler, so if you are reading this and know what you are doing, please give feedback
        [HarmonyPatch(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TranspilerDoPeopleAndAnimals(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            int index = -1;
            //CodeInstruction pawninstruction = null;

            //find the part of the method where packAnimal is read from the pawn's race def
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Is(OpCodes.Ldfld, AccessTools.Field(typeof(RaceProperties), nameof(RaceProperties.packAnimal))))
                {
                    index = i - 1; //want to put the instructions before that index
                    code[i - 1].opcode = OpCodes.Nop; //replace the original call for the packAnimal race property which is done in 2 instructions
                    code[i].opcode = OpCodes.Nop;
                    break;
                }
            }

            var insertedInstructions = new List<CodeInstruction>
            { 
                //the current top value of the stack SHOULD be the pawn we pass to the function
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Db_AllHaulersArePackAnimals), nameof(ShouldCountAsPackAnimal))),
            };

            if (index != -1)
            {
                code.InsertRange(index, insertedInstructions);
            }
            return code.AsEnumerable();
        }
    }
}
