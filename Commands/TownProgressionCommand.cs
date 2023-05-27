using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace Census.Commands
{
	internal class TownProgressionCommand : ModCommand
	{
		public override CommandType Type => CommandType.Console;

		public override string Command => "TownProgress";

		public override string Description => "View Town Progression";

		// public override string Usage => base.Usage;

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			foreach (var townNPCInfo in CensusSystem.instance.realTownNPCsInfos)
			{
				bool present = Main.npc.Any(x=>x.active && x.type == townNPCInfo.type);
				bool spawnable = Main.townNPCCanSpawn[townNPCInfo.type];
				Console.ForegroundColor = present ? ConsoleColor.Green : (spawnable ? ConsoleColor.Yellow : ConsoleColor.Red);
				Console.WriteLine($"{Lang.GetNPCNameValue(townNPCInfo.type)}");
			}
			Console.ResetColor();
		}
	}
}