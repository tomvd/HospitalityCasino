using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityCasino;

public class JoyGiver_PlaySlotMachine : JoyGiver_InteractBuildingInteractionCell
{
    // Allow colonists to use slot machines during parties/gatherings
    public override bool CanDoDuringGathering => true;

    public override Job TryGivePlayJob(Pawn pawn, Thing t)
    {
        if (!VendingMachineJobHelper.CanPawnAffordThis(pawn, t)) return null;
        return base.TryGivePlayJob(pawn, t);
    }
}