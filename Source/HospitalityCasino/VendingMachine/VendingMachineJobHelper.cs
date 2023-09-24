using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HospitalityCasino
{
    public static class VendingMachineJobHelper
	{
		public static List<Building> GetActiveVendingMachines(Map map)
		{
			return map.listerBuildings.allBuildingsColonist
				.Where(building => building.IsVendingMachine()).ToList();
		}

		public static bool IsVendingMachine(this Building building)
		{
			if (building.comps == null) return false;
			return building.comps.Any(comp => comp is CompVendingMachine machine && machine.IsActive() && machine.IsPowered());
		}

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

        public static bool CanPawnAffordThis(Pawn pawn, Thing vendingMachine)
        {
	        CompVendingMachine compVendingMachine = vendingMachine.TryGetComp<CompVendingMachine>();
	        SlotMachineComp comp = vendingMachine.TryGetComp<SlotMachineComp>();
	        if (compVendingMachine != null)
	        {
		        if (CheckIfShouldPay(pawn, vendingMachine))
		        {
			        if (CountSilver(pawn) < compVendingMachine.GetSingleItemPrice(null) * 10)
			        {
				        Log.Message("Pawn wanted to play, but could not afford it");
				        return false;
			        }
		        }
	        }

	        return true;
        }

		public static bool InsertCoin(Pawn pawn, Thing vendingMachineParent)
		{
			CompVendingMachine vendingMachine = vendingMachineParent.TryGetComp<CompVendingMachine>();
			SlotMachineComp comp = vendingMachineParent.TryGetComp<SlotMachineComp>();
			if (vendingMachine != null && CheckIfShouldPay(pawn, vendingMachineParent))
			{
				var cash = pawn.inventory.innerContainer?.FirstOrDefault(t => t?.def == ThingDefOf.Silver);
				if (cash != null && cash.stackCount >= vendingMachine.GetSingleItemPrice(null)){
					//comp.TotalRevenue += vendingMachine.CurrentPrice;
					vendingMachine.ReceivePayment(pawn.inventory.innerContainer, cash, null, 1);
					// TODO - below sound is also played when a poker game is started, which is weird
					MyDefs.Coin.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				}
				else
				{
					return false;
				}
			}
			return true;
		}

		public static void GiveRewardToPawn(Pawn pawn, int amount, bool isGuest, ThingDef rewardDef, CompVendingMachine vendingMachine)
		{
			int payFromMachine;
			int payFromStorage;
            var cashOnMachine = vendingMachine.MainContainer?.FirstOrDefault(t => t?.def == ThingDefOf.Silver);
            if (cashOnMachine == null) {
				payFromMachine = 0;
				payFromStorage = amount;
			} else {
            	payFromMachine = Mathf.Min(cashOnMachine.stackCount, amount);
				payFromStorage = amount - payFromMachine;
			}
			if (payFromMachine > 0) {
				vendingMachine.MainContainer.TryTransferToContainer(cashOnMachine, pawn.inventory.innerContainer, payFromMachine);
				vendingMachine.Payout(payFromMachine);
			}
			if (payFromStorage > 0) {
				// pay rest from silver in storage
                var silverList = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Silver)
                                        .Where(x => !x.Position.Fogged(x.Map) && (pawn.Map.areaManager.Home[x.Position] || x.IsInAnyStorage())).ToList();
				var value = payFromStorage;
                while (value > 0 && silverList.Count > 0)
                {
                    var silver = silverList.First(t => t.stackCount > 0);
                    var num    = Mathf.Min(value, silver.stackCount);
                    silver.SplitOff(num).Destroy();
                    value -= num;
                }
            }			
		}
	}
}