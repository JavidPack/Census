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
		public bool ShowLocatingArrow;

		[DefaultValue(typeof(Color), "173, 255, 47, 255")]
		public Color ArrowColor;

		public bool DisableConditionsText;

		[Header("DeveloperOptions")]
		public bool DisableAutoLocalization;
	}
#pragma warning restore 0649
}
