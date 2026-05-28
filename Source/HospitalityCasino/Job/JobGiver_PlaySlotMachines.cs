using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HospitalityCasino;

/// <summary>
/// Injected into the humanlike think tree via ThinkTreeDef (tag: Humanlike_PostMentalState).
/// Fires for every humanlike pawn; the guards inside TryGiveJob restrict it to
/// friendly non-colonist visitors who are the "gambler type".
/// </summary>
public class JobGiver_PlaySlotMachines : ThinkNode_JobGiver
{
    private static readonly string[] SlotMachineDefs =
    {
        "HC_SlotMachineRed",
        "HC_SlotMachineGreen",
        "HC_SlotMachineBlue"
    };

    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn?.Map == null) return null;

        // --- Guard 1: only friendly, non-player, non-colonist, non-prisoner visitors ---
        if (pawn.Faction == null) return null;
        if (pawn.Faction == Faction.OfPlayer) return null;
        if (pawn.Faction.HostileTo(Faction.OfPlayer)) return null;
        if (pawn.IsColonist || pawn.IsPrisoner) return null;
        if (!pawn.Spawned) return null;

        // --- Guard 2: skip Hospitality mod guests (they gamble via their own joy system) ---
        var lordJob = pawn.GetLord()?.LordJob;
        if (lordJob?.GetType().FullName.StartsWith("Hospitality.") == true) return null;

        // Casino-incident visitors came specifically to play — skip the gambler gate.
        // For everyone else, ~50% are the "gambler type" (stable per pawn).
        bool isCasinoVisitor = pawn.GetLord()?.LordJob is LordJob_CasinoVisit;
        if (!isCasinoVisitor && !Rand.ChanceSeeded(0.5f, pawn.thingIDNumber)) return null;

        // --- Guard 4: ~40% chance to want to play RIGHT NOW (changes every ~1 000 ticks
        //              so the same gambler occasionally wanders rather than looping forever).
        //              Casino visitors always want to play; skip this gate for them too. ---
        if (!isCasinoVisitor && !Rand.ChanceSeeded(0.4f, pawn.thingIDNumber ^ (GenTicks.TicksGame >> 10))) return null;

        // --- Find the nearest reachable, unreserved slot machine ---
        var available = pawn.Map.listerBuildings.allBuildingsColonist
            .Where(b => SlotMachineDefs.Contains(b.def.defName)
                     && b.Spawned
                     && !b.IsBrokenDown()
                     && pawn.CanReach(b, PathEndMode.InteractionCell, Danger.Deadly)
                     && !b.IsForbidden(pawn)
                     && pawn.CanReserve(b, 1, -1, null, false))
            .ToList();

        if (available.Count == 0) return null;

        Building target = available
            .OrderBy(sm => sm.Position.DistanceToSquared(pawn.Position))
            .First();

        return JobMaker.MakeJob(MyDefs.Play_SlotMachine, target);
    }
}
