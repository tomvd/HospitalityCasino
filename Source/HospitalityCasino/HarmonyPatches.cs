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
            HospitalityCasinoMod.harmonyInstance.Patch(AccessTools.Method(typeof(JoyGiver_InteractBuildingSitAdjacent), "TryGivePlayJob"),
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