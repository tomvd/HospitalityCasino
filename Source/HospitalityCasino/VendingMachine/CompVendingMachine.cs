﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityCasino
{
    public class CompVendingMachine : ThingComp, IThingHolder
    {
        private bool isActive;

        private ThingOwner<Thing> silverContainer;

        private int pricing = 2; //free,low,medium,high

        private int lastEmptyTick = 0;
        private float totalSold = 0;

        public ThingOwner<Thing> MainContainer => silverContainer ??= new ThingOwner<Thing>(this, false);

        public int Pricing
        {
            get => pricing;
            set => pricing = value;
        }

        public void SetPricing(int pricing)
        {
            this.pricing = pricing;
        }

        public int Content => MainContainer.TotalStackCount;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isActive, "isActive");
            Scribe_Values.Look(ref pricing, "pricing", 2);
            Scribe_Values.Look(ref lastEmptyTick, "lastEmptyTick");
            Scribe_Values.Look(ref totalSold, "totalSold");
            Scribe_Deep.Look(ref silverContainer, "silverContainer");
        }

        public bool IsActive() => isActive || Properties.noToggle;

        public bool IsPowered()
        {
            CompPowerTrader compPower = this.parent.TryGetComp<CompPowerTrader>();
            if (compPower == null) return true;
            return compPower.PowerOn;
        }

        public bool ShouldBeEmptied()
        {
            if (MainContainer.TotalStackCount == 0) return false;
            if (GenDate.TicksGame > lastEmptyTick + GenDate.TicksPerHour * HospitalityCasinoMod.Settings.EmptyAfterHours) return true;
            return false;
        }

        public void Emptied()
        {
            lastEmptyTick = GenDate.TicksGame;
        }

        public void ReceivePayment(ThingOwner<Thing> inventoryContainer, Thing silver, Thing goods, int amount)
        {
            float price = GetSingleItemPrice(goods) * amount;
            if (price > 0)
            {
                inventoryContainer.TryTransferToContainer(silver, MainContainer, Mathf.CeilToInt(price));
                totalSold += Mathf.CeilToInt(price);
            }
        }
        
        public void Payout(int amount)
        {
            totalSold -= amount;
        }

        public float GetSingleItemPrice(Thing goods)
        {
            if (goods != null)
            {
                return goods.def.BaseMarketValue * Markup();
            }
            else
            {
                return Properties.defaultPrice * MarkupServices();
            }
        }

        private float Markup()
        {
            switch (pricing)
            {
                case 0: // free
                    return 0;
                case 1: // low price
                    return 0.5f;
                case 2: // medium price
                    return 0.75f;
                case 3: // high price
                    return 1f;                
            }

            return 1f;
        }
        
        private float MarkupServices()
        {
            switch (pricing)
            {
                case 0: // free
                    return 0;
                case 1: // low price
                    return 0.5f;
                case 2: // medium price
                    return 1f;
                case 3: // high price
                    return 2f;                
            }

            return 1f;
        }        

        public int MaxCanAfford(Pawn buyerGuest, Thing goods)
        {
            Thing silver = buyerGuest.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            if (silver == null) return 0;
            return (int)(silver.stackCount /GetSingleItemPrice(goods));
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if(!Properties.noToggle)
                yield return new Command_Toggle
                {
                    defaultLabel = "HospitalityCasino_VendingMachine".Translate(),
                    defaultDesc = "HospitalityCasino_VendingMachineToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/VendingMachine"),
                    isActive = IsActive,
                    toggleAction = () => ToggleActive(),
                    disabled = false,
                };

            if (IsActive())
            {
                yield return new Command_SetPricing(this)
                {
                    defaultLabel = "HospitalityCasino_SetPricing".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/pricetag" + pricing),
                    disabled = false
                };
            }           
        }

        public override string CompInspectStringExtra()
        {
            if (IsActive())
            {
                return "HospitalityCasino_VendingMachineContains".Translate() + ((float)Content).ToStringMoney("F0") +
                       "\n"
                       + "HospitalityCasino_VendingMachineTotal".Translate() + ((float)totalSold).ToStringMoney("F0")
                       +"\n";
            }
            else
            {
                return "";
            }
        }

        public bool ToggleActive()
        {
            return isActive = !isActive;
        }
        
        public CompProperties_VendingMachine Properties => (CompProperties_VendingMachine)props;

        public void GetChildHolders(List<IThingHolder> outChildren) { }

        public ThingOwner GetDirectlyHeldThings() => MainContainer;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            MainContainer.TryDropAll(parent.OccupiedRect().Cells.RandomElement(), previousMap, ThingPlaceMode.Near);
            MainContainer.ClearAndDestroyContents();
            base.PostDestroy(mode, previousMap);
        }
        
    }

    public class CompProperties_VendingMachine : CompProperties
    {
        public int defaultPrice = 0;
        public bool noToggle = false;

        public CompProperties_VendingMachine()
        {
            compClass = typeof(CompVendingMachine);
        }
    }
}
