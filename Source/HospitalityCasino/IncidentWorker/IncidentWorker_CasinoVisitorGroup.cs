using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HospitalityCasino;

public class IncidentWorker_CasinoVisitorGroup : IncidentWorker_NeutralGroup
    {
        public override bool FactionCanBeGroupSource(Faction f, IncidentParms parms, bool desperate = false)
        {
            // Only allow friendly or neutral factions to send casino visitors
            return base.FactionCanBeGroupSource(f, parms, desperate) && 
                   !f.HostileTo(Faction.OfPlayer) &&
                   f != Faction.OfPlayer;
        }

        public override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
                return false;

            Map map = (Map)parms.target;
            
            // Only fire if we have slot machines available
            return AvailableSlotMachines(map) > 0;
        }

        public override void ResolveParmsPoints(IncidentParms parms)
        {
            // Set threat points for visitor generation - low value since these are peaceful visitors
            if (parms.points <= 0f)
            {
                parms.points = Rand.Range(45f, 300f);
            }
        }

        private int AvailableSlotMachines(Map map)
        {
            string[] slotMachineDefs = {
                "HC_SlotMachineRed",
                "HC_SlotMachineGreen", 
                "HC_SlotMachineBlue"
            };

            return map.listerBuildings.allBuildingsColonist
                .Count(building => slotMachineDefs.Contains(building.def.defName) && 
                                building.Spawned && 
                                !building.IsBrokenDown());
        }

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int availableSlotMachines = AvailableSlotMachines(map);
            
            if (availableSlotMachines == 0)
                return false;
            
            if (parms.pawnCount > availableSlotMachines) parms.pawnCount = availableSlotMachines;

            // Generate the visitor group
            if (!TryResolveParms(parms))
                return false;

            List<Pawn> visitors = GenerateVisitors(parms, availableSlotMachines);
            if (visitors.NullOrEmpty())
                return false;

            // Spawn them near the edge of the map
            IntVec3 spawnSpot;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnSpot, map, CellFinder.EdgeRoadChance_Neutral))
                return false;

            // Spawn all visitors
            foreach (Pawn visitor in visitors)
            {
                GenSpawn.Spawn(visitor, spawnSpot, map);
                // give them silver
                var money = ThingMaker.MakeThing(ThingDefOf.Silver);
                money.stackCount = 100;
                visitor.inventory.innerContainer.TryAdd(money);
                // make a guest
                //visitor.guest?.SetGuestStatus(Faction.OfPlayer);
                // add joy meter (otherwise it cant do joy stuff)
                Need_Joy joyNeed = visitor.needs.joy;
                if (joyNeed == null)
                {
                    visitor.needs.AddNeed(DefDatabase<NeedDef>.GetNamed("Joy"));
                    joyNeed = visitor.needs.joy; // re-fetch after AddNeed
                }
                if (joyNeed != null)
                    joyNeed.CurLevel = Rand.Range(0, 0.5f);
            }

            // Create the casino visitor lord
            LordMaker.MakeNewLord(parms.faction, new LordJob_CasinoVisit(parms.faction, spawnSpot), map, visitors);

            // Send letter
            Find.LetterStack.ReceiveLetter(this.def.letterLabel, this.def.letterText, this.def.letterDef, visitors[0]);

            return true;
        }

        private List<Pawn> GenerateVisitors(IncidentParms parms, int max)
        {
            List<Pawn> visitors = new List<Pawn>();
            
            // Generate 1-4 casino visitors (cap at 4 regardless of machine count)
            int visitorCount = Rand.Range(1, Mathf.Min(4, max));
            
            for (int i = 0; i < visitorCount; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    PawnKindDefOf.Villager,
                    parms.faction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: false,
                    developmentalStages: DevelopmentalStage.Adult
                );

                Pawn visitor = PawnGenerator.GeneratePawn(request);
                visitors.Add(visitor);
            }

            return visitors;
        }
    }