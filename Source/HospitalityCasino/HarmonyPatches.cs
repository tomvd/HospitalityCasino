using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityCasino
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            // Gate roulette/poker joy givers behind the affordability check.
            // Visitor slot-machine injection is handled by the ThinkTreeDef
            // (HC_SlotMachineVisitor) — no Harmony needed for that path.
            HospitalityCasinoMod.harmonyInstance.Patch(
                AccessTools.Method(typeof(JoyGiver_InteractBuildingSitAdjacent), "TryGivePlayJob"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(TryGivePlayJob_Prefix)));
        }

        static bool TryGivePlayJob_Prefix(Pawn pawn, Thing t, ref Job __result)
        {
            if (!VendingMachineJobHelper.CanPawnAffordThis(pawn, t))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
