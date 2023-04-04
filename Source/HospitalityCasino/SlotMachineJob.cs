using System.Collections.Generic;
using System.Linq;
using Hospitality;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HospitalityCasino
{
	public class JobDriver_PlaySlotMachine : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, job.def.joyMaxParticipants, 0, null, errorOnFailed);
		}

        protected override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetThingA.InteractionCell, PathEndMode.OnCell);
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				job.locomotionUrgency = LocomotionUrgency.Walk;
				TargetThingA.TryGetComp<SlotMachineComp>().eventManager.gameStarted = false;
				TargetThingA.TryGetComp<SlotMachineComp>().eventManager.gamesPlayed = 0;
			};
			toil.tickAction = delegate ()
			{
				SlotMachineComp comp = TargetThingA.TryGetComp<SlotMachineComp>();
				CompPowerTrader compPower = TargetThingA.TryGetComp<CompPowerTrader>();
				CompVendingMachine vendingMachine = TargetThingA.TryGetComp<CompVendingMachine>();
				float extraJoy = 0f;
				if (comp.initialised)
				{
					if (comp.justRespawned)
                    {
						comp.justRespawned = false;
						EndJobWith(JobCondition.Succeeded);
						return;
					}
					if (!compPower.PowerOn) {
						// pawn stops if power goes down
						comp.eventManager.EndGame();
						EndJobWith(JobCondition.Succeeded);
						return;						
					}

					if (!comp.eventManager.gameStarted)
					{
						// small delay between games
						if (comp.eventManager.ticksLeftThisGame > 0) {
							comp.eventManager.ticksLeftThisGame--;
							JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, 1.0f, (Building)TargetThingA);
							return;
						}
						if ((comp.eventManager.gamesPlayed > 3 && (Rand.Chance(0.5f)) || pawn.needs.joy.CurLevel > 0.9f))
						{
							// pawn has a 50% chance to stop after 3 games, or if he has had enough joy
							comp.eventManager.EndGame();
							EndJobWith(JobCondition.Succeeded);
							return;							
						}
						if (!VendingMachineJobHelper.InsertCoin(pawn, TargetThingA)) {
							// pawn stops if he is out of money
							comp.eventManager.EndGame();
							EndJobWith(JobCondition.Succeeded);
							return;
						}	
						comp.eventManager.CalculateNewEvents();
					}
					comp.eventManager.ticksLeftThisGame--;
					if (comp.eventManager.NextEvent(comp.eventManager.ticksLeftThisGame))
					{
						if (comp.eventManager.reelEvents[0].eventType == ReelEventType.Stop)
							MyDefs.MapOnlyDragSlider.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
						comp.eventManager.reelEvents.RemoveAt(0);
						JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, 1.3f, (Building)base.TargetThingA);
						return;
					}
					if (comp.eventManager.reelEvents.Count == 0) {
						// all reels are stopped
						comp.eventManager.gameStarted = false;
						if (comp.eventManager.outcome == SlotGameOutcome.Single)
						{
							int silverRewarded = vendingMachine.CurrentPrice;
							if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
							{
								comp.TotalPayout += silverRewarded;
								VendingMachineJobHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
							}
							extraJoy += 0.2f;
						}								
						if (comp.eventManager.outcome == SlotGameOutcome.Double)
						{
							int silverRewarded = vendingMachine.CurrentPrice*2;
							if (silverRewarded > 0)
							{
								if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
								{
									comp.TotalPayout += silverRewarded;
									VendingMachineJobHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
								}
							}
							extraJoy += 0.5f;
						}							
						if (comp.eventManager.outcome == SlotGameOutcome.Jackpot)
						{
							MyDefs.MapOnlyTinyBell.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
							
							int silverRewarded = 10*vendingMachine.CurrentPrice;
							if (comp.eventManager.slotType==0) {
								pawn.needs.mood.thoughts.memories.TryGainMemory(MyDefs.HC_WonSlotMachineGame);
								extraJoy += 1f;
							}
							if (comp.eventManager.slotType==1) {
								silverRewarded = 100*vendingMachine.CurrentPrice;
								pawn.needs.mood.thoughts.memories.TryGainMemory(MyDefs.HC_WonSlotMachineGameBig);
								extraJoy += 2f;
							}
							if (comp.eventManager.slotType==2) {
								// everything in the machine
					            var cash = vendingMachine.MainContainer?.FirstOrDefault(t => t?.def == ThingDefOf.Silver);
								if (cash == null) silverRewarded = 0;
								else {
									silverRewarded = cash.stackCount;
									pawn.needs.mood.thoughts.memories.TryGainMemory(MyDefs.HC_WonSlotMachineGameBig);
								}
							}
							if (silverRewarded > 0)
							{
								if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
								{
									comp.TotalPayout += silverRewarded;
									VendingMachineJobHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
								}
								extraJoy += 5f;
							}
						}	
						//Log.Message(" revenue=" + comp.TotalRevenue);
						//Log.Message(" payout=" + comp.TotalPayout);

						// pawn stops after 15 games
						if (++comp.eventManager.gamesPlayed > 15) {
							comp.eventManager.EndGame();
							EndJobWith(JobCondition.Succeeded);
							return;						
						}
					}

				}
				if (Find.TickManager.TicksGame > startTick + job.def.joyDuration)
				{
					comp.eventManager.EndGame();
					EndJobWith(JobCondition.Succeeded);
					return;
				}
				JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, 1.0f + extraJoy, (Building)base.TargetThingA);
			};
			
			toil.socialMode = RandomSocialMode.SuperActive;
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = 1200;
			toil.AddFinishAction(delegate
			{
				TargetThingA.TryGetComp<SlotMachineComp>().eventManager.EndGame();
				JoyUtility.TryGainRecRoomThought(this.pawn);
			});
			yield return toil;
		}

		public override object[] TaleParameters()
		{
			return new object[]
			{
				pawn,
				TargetA.Thing.def
			};
		}
	}
	
	public class JoyGiver_PlaySlotMachine : JoyGiver_InteractBuildingInteractionCell
	{
		protected override bool CanDoDuringGathering => true;

		protected override Job TryGivePlayJob(Pawn pawn, Thing t)
		{
			if (!VendingMachineJobHelper.CanPawnAffordThis(pawn, t)) return null;
			return JobMaker.MakeJob(def.jobDef, t);
		}
	}
}