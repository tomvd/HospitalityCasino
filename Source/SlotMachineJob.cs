using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using Hospitality;

namespace HospitalityCasino
{
	public class JobDriver_PlaySlotMachine : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return this.pawn.Reserve(this.job.targetA, this.job, this.job.def.joyMaxParticipants, 0, null, errorOnFailed);
		}

        protected override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
			yield return Toils_Goto.GotoCell(TargetThingA.InteractionCell, PathEndMode.OnCell);
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				this.job.locomotionUrgency = LocomotionUrgency.Walk;
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
						base.EndJobWith(JobCondition.Succeeded);
						return;
					}
					if (!compPower.PowerOn) {
						// pawn stops if power goes down
						comp.eventManager.EndGame();
						base.EndJobWith(JobCondition.Succeeded);
						return;						
					}

					if (!comp.eventManager.gameStarted)
					{
						// small delay between games
						if (comp.eventManager.ticksLeftThisGame > 0) {
							comp.eventManager.ticksLeftThisGame--;
							JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, 1.3f, (Building)base.TargetThingA);
							return;
						}
						if (JobJoyHelper.CheckIfShouldPay(pawn, TargetThingA))
                        {
							var cash = pawn.inventory.innerContainer?.FirstOrDefault(t => t?.def == ThingDefOf.Silver);
							if (cash != null && cash.stackCount >= vendingMachine.CurrentPrice){
								vendingMachine.ReceivePayment(pawn.inventory.innerContainer, cash);
							}
							else
							{
								// pawn stops if out of money
								comp.eventManager.EndGame();
								base.EndJobWith(JobCondition.Succeeded);
								return;
							}
						}
						MyDefs.Coin.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));			
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
							comp.TotalPayout += silverRewarded;
							if (JobJoyHelper.CheckIfShouldPay(pawn, TargetThingA))
							{
								JobJoyHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
							}
							extraJoy += 0.2f;
						}								
						if (comp.eventManager.outcome == SlotGameOutcome.Double)
						{
							int silverRewarded = vendingMachine.CurrentPrice*2;
							comp.TotalPayout += silverRewarded;
							if (silverRewarded > 0)
							{
								if (JobJoyHelper.CheckIfShouldPay(pawn, TargetThingA))
								{
									JobJoyHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
								}
							}
							extraJoy += 0.5f;
						}							
						if (comp.eventManager.outcome == SlotGameOutcome.Jackpot)
						{
							MyDefs.MapOnlyTinyBell.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
							
							int silverRewarded = 10*vendingMachine.CurrentPrice;
							if (comp.eventManager.slotType==0) {
								silverRewarded = 100*vendingMachine.CurrentPrice;
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
								if (JobJoyHelper.CheckIfShouldPay(pawn, TargetThingA))
								{
									JobJoyHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
								}
								extraJoy += 5f;
							}
						}	
						// pawn stops after 10 games
						if (++comp.eventManager.gamesPlayed > 10) {
							comp.eventManager.EndGame();
							base.EndJobWith(JobCondition.Succeeded);
							return;						
						}
					}

				}
				if (Find.TickManager.TicksGame > this.startTick + this.job.def.joyDuration)
				{
					comp.eventManager.EndGame();
					base.EndJobWith(JobCondition.Succeeded);
					return;
				}
				JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, 1.3f + extraJoy, (Building)base.TargetThingA);
			};
			
			toil.socialMode = RandomSocialMode.SuperActive;
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = 1200;
			toil.AddFinishAction(delegate
			{
				JoyUtility.TryGainRecRoomThought(this.pawn);
			});
			yield return toil;
			yield break;
		}

		public override object[] TaleParameters()
		{
			return new object[]
			{
				this.pawn,
				base.TargetA.Thing.def
			};
		}
	}
	
	public class JoyGiver_PlaySlotMachine : JoyGiver_InteractBuildingInteractionCell
	{
		protected override bool CanDoDuringGathering
		{
			get
			{
				return true;
			}
		}
		protected override Job TryGivePlayJob(Pawn pawn, Thing t)
		{
			if (JobJoyHelper.CheckIfShouldPay(pawn, t))
			{
				if (JobJoyHelper.CountSilver(pawn) < t.TryGetComp<CompVendingMachine>().CurrentPrice * 10)
				{
					return null;
				}
				
			}
			return JobMaker.MakeJob(this.def.jobDef, t);
		}
	}
	
	public class JobJoyHelper
	{

		public static bool CheckIfShouldPay(Pawn pawn, Thing slotMachine)
		{
			// only guests pay
			return (pawn.Faction != slotMachine.Faction);
		}
		public static int CountSilver(Pawn pawn)
		{
            if (pawn?.inventory?.innerContainer == null) return 0;
            return pawn.inventory.innerContainer.Where(s => s.def == ThingDefOf.Silver).Sum(s => s.stackCount);
		}
		public static void GiveRewardToPawn(Pawn pawn, int amount, bool isGuest, ThingDef rewardDef, CompVendingMachine vendingMachine)
		{
            var cash = vendingMachine.MainContainer?.FirstOrDefault(t => t?.def == ThingDefOf.Silver);
            if (cash == null) return;
            var payAmount = Mathf.Min(cash.stackCount, amount);
            var paid = vendingMachine.MainContainer.TryTransferToContainer(cash, pawn.inventory.innerContainer, payAmount);
		}
	}
}