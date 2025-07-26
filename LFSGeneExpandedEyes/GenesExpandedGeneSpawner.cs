using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace LFS_GenesExpandedEyes
{
    public class GenesExpandedEyesModSettings: ModSettings
    {
        public bool patchBaseliner = true;
        public bool autoPatchMods = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref patchBaseliner, "patchBaseliner");
            Scribe_Values.Look(ref autoPatchMods, "autoPatchMods");
            base.ExposeData(); 
        }
    }

    public class GenesExpandedEyesMod: Mod
    {
        GenesExpandedEyesModSettings settings;

        public GenesExpandedEyesMod(ModContentPack content): base(content)
        {
            this.settings = GetSettings<GenesExpandedEyesModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Changes to these settings will take effect after restarting the game");
            listingStandard.CheckboxLabeled("Enable Baseliner Xenotype Patching", ref settings.patchBaseliner, "Toggle gene patching for Baseliner xenotypes.");
            listingStandard.CheckboxLabeled("Enable Automatic Patching for Modded Xenotypes", ref settings.autoPatchMods, "Toggle automatic gene patching for xenotypes introduced by other mods without dedicated patches.");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Genes Expanded Eyes";
        }
    }

    [StaticConstructorOnStartup]
    public static class HarmonyPatch
    {
        public static bool patchBaseliner = true;
        public static bool autoPatchMods = true;

        private static readonly Type patchType = typeof(HarmonyPatch);
        private static readonly string defaultAutoPatchName = "DefaultAutoPatch";
        static HarmonyPatch()
        {
            var settings = LoadedModManager.GetMod<GenesExpandedEyesMod>().GetSettings<GenesExpandedEyesModSettings>();
            patchBaseliner = settings.patchBaseliner;
            autoPatchMods = settings.autoPatchMods;

            Harmony harmony = new Harmony("GeneExpandedGeneSpawner");
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), typeof(PawnGenerator).GetMethod(
                        "GenerateGenes",
                        BindingFlags.Static | BindingFlags.NonPublic).Name,
                    null,
                    null),
                null,
                new HarmonyMethod(HarmonyPatch.patchType, "PostfixGenerator", null), null, null);
            Log.Message("[GeneExpandedEyes] harmony patch succeeded.");
        }

        public static void PostfixGenerator(Pawn pawn, XenotypeDef xenotype, PawnGenerationRequest request)
        {
            var customXeno = pawn.genes.UniqueXenotype;
            var isBaby = request.AllowedDevelopmentalStages == DevelopmentalStage.Newborn;
            var isBaseliner = xenotype == XenotypeDefOf.Baseliner;
            
            if (isBaby) return;
            if (customXeno) return;

            GeneGroups possibleEndotypes =
                DefDatabase<GeneGroups>.AllDefs.FirstOrDefault((GeneGroups x) =>
                    string.Equals(x.defName, pawn.genes.Xenotype.defName,
                        StringComparison.CurrentCultureIgnoreCase));

            if(autoPatchMods){
                if (possibleEndotypes == null)
                {
                    possibleEndotypes =
                        DefDatabase<GeneGroups>.AllDefs.FirstOrDefault((GeneGroups x) =>
                            string.Equals(x.defName, defaultAutoPatchName,
                                StringComparison.CurrentCultureIgnoreCase));
                }
            }

            var doesHaveEndoType = possibleEndotypes != null;
            if (!doesHaveEndoType) return;
            if (isBaseliner && !patchBaseliner) return;

            var rnd = new System.Random();
            foreach (var geneGroup in possibleEndotypes.geneGroups)
            {
                var randomNum = rnd.Next(0, geneGroup.Endogenes.Count);
                pawn.genes.AddGene(geneGroup.Endogenes[randomNum].geneDef,
                    false);
            }
        }
    }
}