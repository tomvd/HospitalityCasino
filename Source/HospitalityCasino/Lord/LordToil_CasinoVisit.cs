using RimWorld;
using Verse.AI;
using Verse.AI.Group;

namespace HospitalityCasino;

public class LordToil_CasinoVisit : LordToil
{
    public override void UpdateAllDuties()
    {
        for (int i = 0; i < lord.ownedPawns.Count; i++)
        {
            lord.ownedPawns[i].mindState.duty = new PawnDuty(MyDefs.CasinoVisitor);
        }
    }
}