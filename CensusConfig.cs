using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Census
{
#pragma warning disable 0649
	class CensusConfigClient : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static CensusConfigClient Instance;

		[DefaultValue(true)]
		[Label("Show NPC Locating Arrow")]
		[Tooltip("Hover over an NPC in the housing menu to see which direction the NPC is.\nUseful for finding Town NPC you lost track of.")]
		public bool ShowLocatingArrow;

		[DefaultValue(typeof(Color), "173, 255, 47, 255")]
		[Label("Locating Arrow Color")]
		public Color ArrowColor;

		[Label("Disable Spawn Conditions Text")]
		[Tooltip("Enable this to prevent the spawn condition text from showing.\nUse this if you want to see Town NPCs that could spawn but don't want to be spoiled on how to actually get them to spawn.")]
		public bool DisableConditionsText;

		public override void OnLoaded() {
			Instance = this; // Remove after 0.11.4
		}
	}
#pragma warning restore 0649
}
