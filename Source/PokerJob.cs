
using RimWorld;
using Verse.AI;

namespace HospitalityCasino
{

    public class JobDriver_PlayPokerForMoney : JobDriver_PlayPoker
    {
        protected override void ModifyPlayToil(Toil toil)
        {
            base.ModifyPlayToil(toil);
            toil.initAction = delegate ()
            {
                VendingMachineJobHelper.InsertCoin(toil.GetActor(), base.TargetA.Thing);
            };
        }
    }

}