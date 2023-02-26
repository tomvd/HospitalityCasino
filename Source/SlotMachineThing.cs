using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace HospitalityCasino
{
	public class SlotMachineComp : ThingComp
    {
		public bool initialised = false;
		public ReelEventManager eventManager = new ReelEventManager();
		public bool justRespawned = false;
		public int TotalRevenue = 0; // TODO persist
		public int TotalPayout = 0; // TODO persist
		public CompProperties_SlotMachine Properties => (CompProperties_SlotMachine)props;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
			justRespawned = respawningAfterLoad;
			for (int i = 0; i < 3; i++)
				eventManager.reels[i] = new SlotReel();
			Vector3 middle = parent.Position.ToVector3() + new Vector3(0.5045f, 0f, 0.5f);
			Vector3 offset = new Vector3(0.2f, 0f, 0f);
			middle.z += 0.3f;
			eventManager.reels[0].drawLocation = middle - offset;
			eventManager.reels[1].drawLocation = middle;
			eventManager.reels[2].drawLocation = middle + offset;
			eventManager.reels[0].drawLocation.y = 5.5f;
			eventManager.reels[1].drawLocation.y = 5.5f;
			eventManager.reels[2].drawLocation.y = 5.5f;
			eventManager.gameStarted = false;
			eventManager.slotType = Properties.type;
			initialised = true;
		}
        public override void PostDeSpawn(Map map)
		{
			initialised = false;
			base.PostDeSpawn(map);
        }
        public override void CompTick()
        {
            base.CompTick();
			HospitalityCasinoMod.InitialiseTextures();
			foreach(SlotReel reel in eventManager.reels)
            {
				reel.UpdateDrawState();
			}
        }
        public override void PostDraw()
        {
            base.PostDraw();
			if (initialised && parent.Rotation == Rot4.South)
			{
				Matrix4x4 matrix1 = default(Matrix4x4);
                matrix1.SetTRS(eventManager.reels[0].drawLocation, Rot4.North.AsQuat, Vector3.one);
                Graphics.DrawMesh(eventManager.reels[0].drawingMesh, matrix1, eventManager.reels[0].drawingMaterial, 0);
                Matrix4x4 matrix2 = default(Matrix4x4);
                matrix2.SetTRS(eventManager.reels[1].drawLocation, Rot4.North.AsQuat, Vector3.one);
                Graphics.DrawMesh(eventManager.reels[1].drawingMesh, matrix2, eventManager.reels[1].drawingMaterial, 0);
                Matrix4x4 matrix3 = default(Matrix4x4);
                matrix3.SetTRS(eventManager.reels[2].drawLocation, Rot4.North.AsQuat, Vector3.one);
                Graphics.DrawMesh(eventManager.reels[2].drawingMesh, matrix3, eventManager.reels[2].drawingMaterial, 0);
				justRespawned = false;
			}
		}
	}

	public class CompProperties_SlotMachine : CompProperties
    {
        public int type = 0;

        public CompProperties_SlotMachine()
        {
            compClass = typeof(SlotMachineComp);
        }
    }

	public enum SlotReelState { Fruit, Bar, Seven, Spinning }
	public class SlotReel
	{
		public SlotReelState state = SlotReelState.Seven;
		public SlotReelState endState = SlotReelState.Seven;
		public Vector3 drawLocation;
		public Mesh drawingMesh;
		public Material drawingMaterial;
		public SlotReel()
        {

        }
		public void UpdateDrawState()
        {
			switch(state)
			{
				case SlotReelState.Seven:
					drawingMaterial = HospitalityCasinoMod.reelMaterials[0];
					drawingMesh = HospitalityCasinoMod.reelMeshes[0];
					break;
				case SlotReelState.Fruit:
					drawingMaterial = HospitalityCasinoMod.reelMaterials[1];
					drawingMesh = HospitalityCasinoMod.reelMeshes[1];
					break;
				case SlotReelState.Bar:
					drawingMaterial = HospitalityCasinoMod.reelMaterials[2];
					drawingMesh = HospitalityCasinoMod.reelMeshes[2];
					break;					
				case SlotReelState.Spinning:
					int thisFrame = Rand.Range(3, 3 + HospitalityCasinoMod.blurFrameCount);
					drawingMaterial = HospitalityCasinoMod.reelMaterials[thisFrame];
					drawingMesh = HospitalityCasinoMod.reelMeshes[thisFrame];
					break;
			}
        }
    }
	
	/* state machine for the reels */
	public enum SlotGameOutcome { Undefined, Loss, Single, Double, Jackpot }
	public class ReelEventManager
    {
		public List<ReelEvent> reelEvents = new List<ReelEvent>();
		public SlotReel[] reels = new SlotReel[3];
		public bool gameStarted = false;
		public int gamesPlayed = 0;
		public int slotType = 0;
		public int ticksLeftThisGame = 0;
		public SlotGameOutcome outcome = SlotGameOutcome.Undefined;

		public bool NextEvent(int tickNow)
        {
			if (reelEvents.Count > 0)
            {
				return reelEvents[0].AttemptFireEvent(tickNow);
            }
			return false;
        }
		public void EndGame()
        {
			reelEvents.Clear();
			outcome = SlotGameOutcome.Undefined;
			foreach(SlotReel reel in reels)
            {
				reel.state = SlotReelState.Bar;
				reel.UpdateDrawState();
            }
        }
		public void CalculateNewEvents()
        {
			// outcome is defined here, as well as the reels endstate
			float houseCut = 6f; // always between 0 and 12, default 6
			outcome = SlotGameOutcome.Loss;
			float random = Rand.Range(0f, 1f);
			if (slotType==0) {
				if (random <= 0.455f) outcome = SlotGameOutcome.Single; // 34% chance of single bet win = 1s
				if (random <= (0.105f + Mathf.Lerp(+0.005f,-0.005f,(houseCut*100f/12f)))) outcome = SlotGameOutcome.Double; // 5.5 to 4.5% (variable depending on house cut 0-12%) chance of double bet win = 2s
				if (random <= (0.05f + Mathf.Lerp(+0.0005f,-0.0005f,(houseCut*100f/12f)))) outcome = SlotGameOutcome.Jackpot; // 0.55 to 0.45% (variable depending on house cut 0-12%) chance of jackpot = all in machine, capped to 100s
			}
			if (slotType==1 || slotType==2) {
				if (random <= 0.395f) outcome = SlotGameOutcome.Single; // 34% chance of single bet win = 1s
				if (random <= (0.055f + Mathf.Lerp(+0.005f,-0.005f,(houseCut*100f/12f)))) outcome = SlotGameOutcome.Double; // 5.5 to 4.5% (variable depending on house cut 0-12%) chance of double bet win = 2s
				if (random <= (0.005f + Mathf.Lerp(+0.0005f,-0.0005f,(houseCut*100f/12f)))) outcome = SlotGameOutcome.Jackpot; // 0.55 to 0.45% (variable depending on house cut 0-12%) chance of jackpot = all in machine, capped to 100s
			}
			Log.Message("CalculateNewEvents outcome=" + outcome);
			switch(outcome) {
				case SlotGameOutcome.Loss:
					// TODO any random combination where no 3 same
					reels[0].endState = getRandomReelState();
					reels[1].endState = SlotReelState.Bar;
					reels[2].endState = SlotReelState.Fruit;
					break;
				case SlotGameOutcome.Single:
					reels[0].endState = SlotReelState.Fruit;
					reels[1].endState = SlotReelState.Fruit;
					reels[2].endState = SlotReelState.Fruit;
					break;
				case SlotGameOutcome.Double:
					reels[0].endState = SlotReelState.Bar;
					reels[1].endState = SlotReelState.Bar;
					reels[2].endState = SlotReelState.Bar;
					break;
				case SlotGameOutcome.Jackpot:
					reels[0].endState = SlotReelState.Seven;
					reels[1].endState = SlotReelState.Seven;
					reels[2].endState = SlotReelState.Seven;
					break;					
			}
			
			ticksLeftThisGame = 120;// each game takes 2 seconds
			int ticksLength = ticksLeftThisGame; 
			reelEvents.Clear();
			reelEvents.Add(new ReelEvent(ticksLength, reels[0], ReelEventType.Start));
			reelEvents.Add(new ReelEvent(ticksLength, reels[1], ReelEventType.Start));
			reelEvents.Add(new ReelEvent(ticksLength, reels[2], ReelEventType.Start));
			ticksLength = 60;
			reelEvents.Add(new ReelEvent(ticksLength, reels[0], ReelEventType.Stop));
			ticksLength = 40;
			reelEvents.Add(new ReelEvent(ticksLength, reels[1], ReelEventType.Stop));
			ticksLength = Rand.Range(15, 25);
			reelEvents.Add(new ReelEvent(ticksLength, reels[2], ReelEventType.Stop));
			gameStarted = true;
        }

		private SlotReelState getRandomReelState() {
			float random = Rand.Range(0f, 1f);
			SlotReelState state = SlotReelState.Fruit;
			if (random < 66) state = SlotReelState.Bar;
			if (random < 33) state = SlotReelState.Seven;
			return state;
		}
	
    }


	public enum ReelEventType { Start, Stop }
	public class ReelEvent
    {
		public int tickHappeningOn;
		public bool eventFiredYet = false;
		public ReelEventType eventType;
		public SlotReel reel;
		public ReelEvent(int tickHappeningOn, SlotReel reel, ReelEventType type)
        {
			this.tickHappeningOn = tickHappeningOn;
			eventType = type;
			this.reel = reel;
        }
		public bool AttemptFireEvent(int tickNow)
        {
			if (tickNow <= tickHappeningOn) // last 20 ticks is just a delay
			{
				if (eventType == ReelEventType.Start)
					reel.state = SlotReelState.Spinning;
				if (eventType == ReelEventType.Stop)
					reel.state = reel.endState;
				reel.UpdateDrawState();
				return true;
			}
			return false;
        }
	}	
}