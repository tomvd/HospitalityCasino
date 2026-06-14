using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HospitalityCasino;

/// <summary>
/// Injected into the humanlike think tree via ThinkTreeDef (tag: Humanlike_PostMentalState).
/// Fires for every humanlike pawn; restricted to active LordJob_CasinoVisit pawns only.
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

        // --- Guard 1b: don't pull patients away from medical care (downed, bedridden, or
        // suffering from any injury/infection that needs tending) ---
        if (pawn.Downed) return null;
        if (pawn.InBed()) return null;
        if (pawn.health?.hediffSet?.HasTendableHediff() == true) return null;

        // --- Guard 2: skip Hospitality mod guests (they gamble via their own joy system) ---
        var lordJob = pawn.GetLord()?.LordJob;
        if (lordJob?.GetType().FullName.StartsWith("Hospitality.") == true) return null;

        // Don't pull ANY visitor away from an exit toil — this caused casino visitors to turn
        // around at the map edge and loop back to play more slots indefinitely.
        if (pawn.GetLord()?.CurLordToil is LordToil_ExitMap) return null;

        bool isCasinoVisitor = lordJob is LordJob_CasinoVisit;

        if (!isCasinoVisitor)
        {
            // ~50% of non-casino visitors are the "gambler type" (stable per pawn).
            if (!Rand.ChanceSeeded(0.5f, pawn.thingIDNumber)) return null;
            // ~30% chance to want to play right now (reseeds every ~1 000 ticks).
            if (!Rand.ChanceSeeded(0.3f, pawn.thingIDNumber ^ (GenTicks.TicksGame >> 10))) return null;
            // Don't play when hungry or tired — the job giver fires at PostMentalState priority,
            // which is above need-satisfaction in the think tree, so we guard here instead.
            if (pawn.needs?.food?.CurLevel < 0.4f) return null;
            if (pawn.needs?.rest?.CurLevel < 0.3f) return null;
        }

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
