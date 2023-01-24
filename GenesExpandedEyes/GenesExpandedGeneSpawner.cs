using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LFS_GenesExpandedEyes
{
    [StaticConstructorOnStartup]
    public static class GeneExpandedGeneSpawner
    {
        private static readonly Type patchType = typeof(GeneExpandedGeneSpawner);

        static GeneExpandedGeneSpawner()
        {
            Harmony _harmony = new Harmony("GeneExpandedGeneSpawner");
            _harmony.Patch(
                AccessTools.Method(typeof(PawnGenerator),
                    typeof(PawnGenerator).GetMethod("GenerateGenes", BindingFlags.Static | BindingFlags.NonPublic).Name,
                    null, null), null, new HarmonyMethod(GeneExpandedGeneSpawner.patchType, "PostFixGenes", null),
                null, null);
        }

        private static void PostFixGenes(Pawn pawn, XenotypeDef xenotypeDef, PawnGenerationRequest request)
        {
            var isBaby = request.AllowedDevelopmentalStages == DevelopmentalStage.Newborn;

            if (!isBaby)
            {
                var customXeno = pawn.genes.UniqueXenotype;
                if (!customXeno)
                {
                    GeneGroups possibleEndotypes =
                        DefDatabase<GeneGroups>.AllDefs.FirstOrDefault((GeneGroups x) =>
                            string.Equals(x.defName, pawn.genes.Xenotype.defName,
                                StringComparison.CurrentCultureIgnoreCase));

                    bool doesHaveEndoType = possibleEndotypes != null;
                    if (doesHaveEndoType)
                    {
                        foreach (var geneGroup in possibleEndotypes.geneGroups)
                        {
                            var rnd = new Random();
                            pawn.genes.AddGene(geneGroup.genes[rnd.Next(0, geneGroup.genes.Count)], false);
                        }
                    }
                }
            }
        }
    }
}