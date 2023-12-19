using System.IO;
using Terraria.ModLoader;

namespace Census
{
	// TODO: sync WorldGen.prioritizedTownNPC and Main.townNPCCanSpawn[townNPCInfo.type] for MP clients.
	// manual check button?
	// cheat for heros mod.
	internal class Census : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			CensusSystem.instance.HandlePacket(reader, whoAmI);
		}

		// string:"TownNPCCondition" - int:npcid - string:condition
		public override object Call(params object[] args) {
			if(CensusSystem.instance == null) {
				Logger.Error("Call was called before CensusSystem class loaded. Make sure to only use Call during or after Mod.PostSetupContent.");
				return "Failure";
			}
			return CensusSystem.instance.Call(args);
		}

		//public override void HandlePacket(BinaryReader reader, int whoAmI)
		//{
		//	CensusMessageType msgType = (CensusMessageType)reader.ReadByte();
		//	switch (msgType)
		//	{
		//		default:
		//			ErrorLogger.Log("Census: Unknown Message type: " + msgType);
		//			break;
		//	}
		//}
	}

	enum CensusMessageType : byte
	{
		CensusInfo
	}
}

