using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HospitalityCasino;

[StaticConstructorOnStartup]
public class Command_SetPricing : Command
{
	private CompVendingMachine vendingMachine;

	public Command_SetPricing(CompVendingMachine vendingMachine)
	{
		this.vendingMachine = vendingMachine;
		switch (vendingMachine.Pricing)
		{
		case 0:
			defaultLabel = "HospitalityCasino_Free".Translate();
			break;
		case 1:
			defaultLabel = "HospitalityCasino_Low".Translate();
			break;
		case 2:
			defaultLabel = "HospitalityCasino_Medium".Translate();
			break;
		case 3:
			defaultLabel = "HospitalityCasino_High".Translate();
			break;		
		default:
			Log.Error($"Unknown pricing selected for : {vendingMachine.Pricing}");
			break;
		}
		defaultDesc = "HospitalityCasino_SetPricing".Translate();
	}

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		list.Add(new FloatMenuOption("HospitalityCasino_Free".Translate(), delegate
		{
			vendingMachine.SetPricing(0);
		}, (Texture2D)null, Color.white));
		list.Add(new FloatMenuOption("HospitalityCasino_Low".Translate(), delegate
		{
			vendingMachine.SetPricing(1);
		}, (Texture2D)null, Color.white));
		list.Add(new FloatMenuOption("HospitalityCasino_Medium".Translate(), delegate
		{
			vendingMachine.SetPricing(2);
		}, (Texture2D)null, Color.white));
		list.Add(new FloatMenuOption("HospitalityCasino_High".Translate(), delegate
		{
			vendingMachine.SetPricing(3);
		}, (Texture2D)null, Color.white));
		Find.WindowStack.Add(new FloatMenu(list));
	}
}