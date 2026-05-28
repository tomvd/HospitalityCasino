using System.Collections.Generic;
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

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetThingA.InteractionCell, PathEndMode.OnCell);
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				job.locomotionUrgency = LocomotionUrgency.Jog;
				TargetThingA.TryGetComp<SlotMachineComp>().eventManager.gameStarted = false;
				TargetThingA.TryGetComp<SlotMachineComp>().eventManager.gamesPlayed = 0;
			};
			toil.tickAction = delegate
			{
				SlotMachineComp comp = TargetThingA.TryGetComp<SlotMachineComp>();
				CompPowerTrader compPower = TargetThingA.TryGetComp<CompPowerTrader>();
				CompVendingMachine vendingMachine = TargetThingA.TryGetComp<CompVendingMachine>();
				float extraJoy = 0f;
				pawn.GainComfortFromCellIfPossible(1);
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
							JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1.0f, (Building)TargetThingA);
							return;
						}
						float joyLevel = pawn.needs?.joy?.CurLevel ?? 0f;
						// Joy-less pawns (no joy need) rely solely on the 10% random quit,
						// which gives ~13 expected games. Hard-cap at 5 so they don't
						// monopolise a machine for the entire session.
						if (pawn.needs?.joy == null && comp.eventManager.gamesPlayed > 5)
						{
							comp.eventManager.EndGame();
							EndJobWith(JobCondition.Succeeded);
							return;
						}
						if ((comp.eventManager.gamesPlayed > 3 && Rand.Chance(0.1f)) || joyLevel > 0.9f)
						{
							// pawn has a 10% chance to stop after 3 games, or if he has had enough joy, unless he is an addict
							Trait drugDesire = pawn.story?.traits.GetTrait(TraitDefOf.DrugDesire);
							if (drugDesire == null || drugDesire.Degree == 0)
							{
								comp.eventManager.EndGame();
								EndJobWith(JobCondition.Succeeded);
								return;
							}
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
							MyDefs.MapOnlyDragSlider.PlayOneShot(SoundInfo.InMap(pawn));
						comp.eventManager.reelEvents.RemoveAt(0);
						JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.None, 1.3f, (Building)TargetThingA);
						return;
					}
					if (comp.eventManager.reelEvents.Count == 0) {
						// all reels are stopped
						comp.eventManager.gameStarted = false;
						if (comp.eventManager.outcome == SlotGameOutcome.Single)
						{
							int silverRewarded = 2 * (comp.Properties.type + 1);
							if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
							{
								comp.TotalPayout += silverRewarded;
								VendingMachineJobHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
							}
							extraJoy += 0.2f * (comp.Properties.type + 1);
						}								
						if (comp.eventManager.outcome == SlotGameOutcome.Double)
						{
							int silverRewarded = 2 * (comp.Properties.type + 1) * 2;
							if (silverRewarded > 0)
							{
								if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
								{
									comp.TotalPayout += silverRewarded;
									VendingMachineJobHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
								}
							}
							extraJoy += 0.5f * (comp.Properties.type + 1);
						}							
						if (comp.eventManager.outcome == SlotGameOutcome.Jackpot)
						{
							MyDefs.MapOnlyTinyBell.PlayOneShot(SoundInfo.InMap(pawn));
							
							int silverRewarded = 2 * (comp.Properties.type + 1) * 5;
							if (comp.eventManager.slotType==0) {
								pawn.needs?.mood?.thoughts.memories.TryGainMemory(MyDefs.HC_WonSlotMachineGame);
								extraJoy += 1f;
							}
							if (silverRewarded > 0)
							{
								if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
								{
									comp.TotalPayout += silverRewarded;
									VendingMachineJobHelper.GiveRewardToPawn(pawn, silverRewarded, pawn.Faction != TargetThingA.Faction, ThingDefOf.Silver, vendingMachine);
								}
								extraJoy += 2f * (comp.Properties.type + 1);
							}
						}	
						//Log.Message(" revenue=" + comp.TotalRevenue);
						//Log.Message(" payout=" + comp.TotalPayout);
					}

				}
				/*if (Find.TickManager.TicksGame > startTick + job.def.joyDuration)
				{
					comp.eventManager.EndGame();
					EndJobWith(JobCondition.Succeeded);
					return;
				}*/
				JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1.0f + extraJoy, (Building)TargetThingA);
			};
			
			toil.socialMode = RandomSocialMode.SuperActive;
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = 1200;
			toil.AddFinishAction(delegate
			{
				TargetThingA.TryGetComp<SlotMachineComp>().eventManager.EndGame();
				JoyUtility.TryGainRecRoomThought(this.pawn);
				if (VendingMachineJobHelper.CheckIfShouldPay(pawn, TargetThingA))
				{
					// guests gain thoughts about non-default settings of slot machines
					if (TargetThingA.TryGetComp<CompVendingMachine>().Pricing == 3)
					{
						toil.actor.needs?.mood?.thoughts.memories.TryGainMemory(MyDefs.RiggedSlotmachine);
					}
					else if (TargetThingA.TryGetComp<CompVendingMachine>().Pricing == 1)
					{
						toil.actor.needs?.mood?.thoughts.memories.TryGainMemory(MyDefs.GenerousSlotmachine);
					}
				}
			});
			yield return toil;
		}

		public override object[] TaleParameters()
		{
			return
			[
				pawn,
				TargetA.Thing.def
			];
		}
	}
}