using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityCasino
{
	[DefOf]
	public class MyDefs
	{
		static MyDefs()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(MyDefs));
		}

		public static ThoughtDef HC_WonSlotMachineGame;
		public static ThoughtDef HC_WonSlotMachineGameBig;
		public static SoundDef MapOnlyDragSlider;
		public static SoundDef MapOnlyTinyBell;
		public static SoundDef Coin;

		public static ThingDef Mote_ReelBlurA;
		public static ThingDef Mote_ReelBlurB;
		public static ThingDef Mote_ReelBlurC;
		public static ThingDef Mote_ReelOutcomeSeven;
		public static ThingDef Mote_ReelOutcomeFruit;
		public static ThingDef Mote_ReelOutcomeBar;
	}
	
	[StaticConstructorOnStartup]
	public class HospitalityCasinoMod : Mod
	{
		//public static Settings settings; // TODO perhaps... some day...
		public static ThingDef[] reelFrameThingDefs;
		public static Thing[] reelFrameThings;
		public static Mesh[] reelMeshes;
		public static Material[] reelMaterials;
		public static int blurFrameCount;
		public static bool initialised = false;
		public HospitalityCasinoMod(ModContentPack content) : base(content)
		{
			//InitialiseTextures();
			// settings
            harmonyInstance = new Harmony("Adamas.HospitalityCasino");
            harmonyInstance.PatchAll();
        }

        public static Harmony harmonyInstance;			
		
		
		public static void InitialiseTextures()
        {
			if (!initialised)
			{
				initialised = true;
				reelFrameThingDefs = new ThingDef[]
				{
					MyDefs.Mote_ReelOutcomeSeven,
					MyDefs.Mote_ReelOutcomeFruit,
					MyDefs.Mote_ReelOutcomeBar,
					MyDefs.Mote_ReelBlurA,
					MyDefs.Mote_ReelBlurB,
					MyDefs.Mote_ReelBlurC
				};
				blurFrameCount = reelFrameThingDefs.Length - 3;
				reelFrameThings = new Thing[reelFrameThingDefs.Length];
				reelMeshes = new Mesh[reelFrameThingDefs.Length];
				reelMaterials = new Material[reelFrameThingDefs.Length];
				for (int i = 0; i < reelFrameThings.Length; i++)
				{
					reelFrameThings[i] = ThingMaker.MakeThing(reelFrameThingDefs[i]);
					reelMeshes[i] = reelFrameThings[i].Graphic.MeshAt(Rot4.North);
					reelMaterials[i] = reelFrameThings[i].Graphic.MatSingle;
				}
				//MyDefs.WonSlotMachineGame.durationDays = settings.moodBoostDurationDays;
				//MyDefs.WonSlotMachineGame.stackLimit = settings.moodBoostStackAmount;
			}
        }
	}
}