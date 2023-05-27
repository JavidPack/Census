using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Census
{
	internal class TownNPCInfo
	{
		public int type;
		public LocalizedText conditions;

		public TownNPCInfo(int type, LocalizedText conditions = null) {
			this.type = type;
			this.conditions = conditions;
			if(this.conditions == null) {
				if (type < NPCID.Count) {
					string key = $"Mods.Census.SpawnConditions.{NPCID.Search.GetName(type)}";
					if (Language.Exists(key))
						this.conditions = Language.GetText(key);
					else
						this.conditions = Language.GetText("Mods.Census.SpawnConditions.Unknown");
				}
				else {
					// This shouldn't happen.
					this.conditions = Language.GetText("Mods.Census.SpawnConditions.Unknown");
				}
			}
		}

		public TownNPCInfo(ModNPC modNPC) {
			// No localization provided, use automatic.
			this.type = modNPC.Type;
			// Default value is english. Code will register automatically.
			this.conditions = modNPC.GetLocalization("Census_SpawnCondition", () => "Conditions unknown"); 
		}

		//public void Deconstruct(out int type, out string conditions) {
		//	type = this.type;
		//	conditions = this.conditions;
		//}
	}
}

