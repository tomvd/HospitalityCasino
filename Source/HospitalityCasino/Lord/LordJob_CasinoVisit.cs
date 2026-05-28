using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HospitalityCasino;

public class LordJob_CasinoVisit : LordJob
{
    private Faction faction;
    private IntVec3 enterSpot;

    public LordJob_CasinoVisit()
    {
    }

    public LordJob_CasinoVisit(Faction faction, IntVec3 enterSpot)
    {
        this.faction = faction;
        this.enterSpot = enterSpot;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph stateGraph = new StateGraph();

        // Casino visit state - where visitors play slot machines
        LordToil_CasinoVisit casinoVisit = new LordToil_CasinoVisit();
        stateGraph.StartingToil = casinoVisit;

        // Leave state - visitors head to the exit
        LordToil_ExitMap exitMap = new LordToil_ExitMap(LocomotionUrgency.Walk, canDig: false);

        // Transition from casino visit to exit after some time (TODO or out of money or other reasons?)
        Transition casinoToExit = new Transition(casinoVisit, exitMap);
        casinoToExit.AddTrigger(new Trigger_TicksPassedWithoutHarm(Rand.Range(GenDate.TicksPerHour*6, GenDate.TicksPerHour*8)));
        casinoToExit.AddPostAction(new TransitionAction_Message("CasinoVisitorsLeavingTime".Translate()));

        // Transition from casino visit to exit after out of money
        Transition casinoToExit2 = new Transition(casinoVisit, exitMap);
        casinoToExit2.AddTrigger(new Trigger_TickCondition(() => lord.ownedPawns.All(pawn => VendingMachineJobHelper.CountSilver(pawn) < 10)));
        casinoToExit2.AddPostAction(new TransitionAction_Message("CasinoVisitorsLeavingMoney".Translate()));

        

        //stateGraph.AddToil(casinoVisit);
        stateGraph.AddToil(exitMap);
        stateGraph.AddTransition(casinoToExit);
        stateGraph.AddTransition(casinoToExit2);

        return stateGraph;
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref enterSpot, "enterSpot");
    }
}
