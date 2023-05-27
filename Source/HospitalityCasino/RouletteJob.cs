
using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityCasino
{

    public class JobDriver_PlayRouletteForMoney : JobDriver_SitFacingBuilding
    {
        protected override void ModifyPlayToil(Toil toil)
        {
            base.ModifyPlayToil(toil);
            toil.initAction = delegate ()
            {
                VendingMachineJobHelper.InsertCoin(toil.GetActor(), base.TargetA.Thing);
            };
            toil.WithEffect((Func<EffecterDef>) (() => VanillaFurnitureEC.EffecterDefOf.Joy_HoldChips), (Func<LocalTargetInfo>) (() => (LocalTargetInfo) this.TargetA.Thing.OccupiedRect().ClosestCellTo(this.pawn.Position)));

        }
    }

}