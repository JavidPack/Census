using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;
using Terraria.GameContent;

namespace Census
{
	internal class TownNPCInfo
	{
		public int type;
		public string conditions;

		public TownNPCInfo(int type, string conditions)
		{
			this.type = type;
			this.conditions = conditions;
		}

		public void Deconstruct(out int type, out string conditions)
		{
			type = this.type;
			conditions = this.conditions;
		}
	}

	internal class CensusWorld : ModSystem
	{
		public override void OnWorldLoad() {
			Census.calculated = false;
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			Census.instance.ModifyInterfaceLayers(layers);
		}
	}

	// TODO: sync WorldGen.prioritizedTownNPC and Main.townNPCCanSpawn[townNPCInfo.type] for MP clients.
	// manual check button?
	// cheat for heros mod.
	internal class Census : Mod
	{
		internal static Census instance;
		internal static bool calculated;

		public override void Load()
		{
			/*
			 * WorldGen.spawnDelay controls when TrySpawningTownNPC->SpawnTownNPC is called. 20 ticks in updateworld. random tiles until a house is found.
			 * 
			 * 
			 * 
			 * WorldGen.prioritizedTownNPC seems to be next to spawn
			 * UpdateTime_SpawnTownNPCs has some conditions we can adapt, order also. called each frame during daytime. Main.checkForSpawns means check only happens every 7200 ticks. Every 2 min.
			 * Main.townNPCCanSpawn means conditions met.
			 * SpawnTownNPC first checks IsThereASpawnablePrioritizedTownNPC which fixes prioritizedTownNPC if it needs to
			 * 
			 * Show Green -> in town, 
			 * blue next
			 * yellow conditions met.
			 * Red townNPCCanSpawn false
			 * 
			 * if (npc.npc.townNPC && NPC.TypeToHeadIndex(npc.npc.type) >= 0  --> Real check for townNPC
			 * 
			 * or checkmark, x mark, over townnpc heads?
			 * 
			 * 
			 * Order: TownNPC and then `foreach (ModNPC npc in npcs) {`
			 * 
			 */
			instance = this;
			calculated = false;
			//modTownNPCsInfos = new List<TownNPCInfo>();

			IL.Terraria.Main.UpdateTime_SpawnTownNPCs += Main_UpdateTime_SpawnTownNPCs;
		}

		private void Main_UpdateTime_SpawnTownNPCs(ILContext il) {
			var c = new ILCursor(il);
			c.GotoNext(i => i.MatchCall(typeof(NPCLoader), nameof(NPCLoader.CanTownNPCSpawn)));
			c.Index++;
			c.EmitDelegate<Action>(() => {
				Census.calculated = true;
				if (Main.dedServ) {
					var packet = GetPacket();
					packet.Write((byte)CensusMessageType.CensusInfo);

					packet.Write(WorldGen.prioritizedTownNPCType);
					packet.Write(Main.townNPCCanSpawn.Length);

					//var compressed = BitsByte.ComposeBitsBytesChain(false, Main.townNPCCanSpawn);
					//foreach (var bitsByte in compressed) {
					//	packet.Write(bitsByte);
					//}
					for (int i = 0; i < Main.townNPCCanSpawn.Length; i += 8) {
						var bits = new BitsByte();
						for (int j = 0; j < 8 && j + i < Main.townNPCCanSpawn.Length; j++) {
							bits[j] = Main.townNPCCanSpawn[j + i];
						}
						packet.Write(bits);
					}
					//for (int i = 0; i < Main.townNPCCanSpawn.Length; i++) {
					//	packet.Write(Main.townNPCCanSpawn[i]);
					//}
					packet.Send();
				}
			});
		}

		internal List<TownNPCInfo> realTownNPCsInfos;
		List<TownNPCInfo> modTownNPCsInfos = new List<TownNPCInfo>();
		public override void PostAddRecipes()
		{
			// By this point, all NPC of all mods are loaded. (and setdefaults)
			//townTracker = new TownTracker();

			//realTownNPCs = new List<int>() { 22, 17, 18, 19,107, 441,
			//	108, 160, 20, 38, 228,
			//	178, 124, 369,
			//	209, 229, 54, 353,
			//	207, 227, 208,142, 550 };

			realTownNPCsInfos = new List<TownNPCInfo>()
			{
				new TownNPCInfo(NPCID.Guide, "Always available, spawned on world generation"),
				new TownNPCInfo(NPCID.Merchant, $"Have [i/s50:{ItemID.SilverCoin}] or more in inventory"),
				new TownNPCInfo(NPCID.Nurse, "Have more than 100 HP and the Merchant present"),
				new TownNPCInfo(NPCID.Demolitionist, "Have any explosive in inventory and the Merchant present"),
				new TownNPCInfo(NPCID.DyeTrader, "Have a strange plant in inventory, or have any dye item in inventory after a boss has been defeated"),
				new TownNPCInfo(NPCID.Angler, "Find at the ocean"),
				new TownNPCInfo(NPCID.BestiaryGirl, "Fill at least 10% of the bestiary"),
				new TownNPCInfo(NPCID.Dryad, "When any boss is defeated"),
				new TownNPCInfo(NPCID.Painter, "Acquire 8 other townspeople"),
				new TownNPCInfo(NPCID.Golfer, "Find in the underground desert"),
				new TownNPCInfo(NPCID.ArmsDealer, "Have bullets or a gun in your inventory"),
				new TownNPCInfo(NPCID.DD2Bartender, "When Eater of World or Brain of Cthulhu has been defeated, find in world"),
				new TownNPCInfo(NPCID.Stylist, "Find in a spider nest"),
				new TownNPCInfo(NPCID.GoblinTinkerer, "Find in the cavern layer after a goblin invasion has been defeated"),
				new TownNPCInfo(NPCID.WitchDoctor, "When Queen Bee has been defeated"),
				new TownNPCInfo(NPCID.Clothier, "When Skeletron has been defeated"),
				new TownNPCInfo(NPCID.Mechanic, "Find in the dungeon"),
				new TownNPCInfo(NPCID.PartyGirl, "1/40 chance after acquiring 14 other townspeople"),
				new TownNPCInfo(NPCID.Wizard, "Find in the cavern layer in hardmode"),
				new TownNPCInfo(NPCID.TaxCollector, $"In hardmode, purify tortured soul with [i:{ItemID.PurificationPowder}] in the underworld"),
				new TownNPCInfo(NPCID.Truffle, "In hardmode, build a house in an above ground mushroom biome"),
				new TownNPCInfo(NPCID.Pirate, "When a Pirate invasion has been defeated"),
				new TownNPCInfo(NPCID.Steampunker, "When any Mechanical boss has been defeated"),
				new TownNPCInfo(NPCID.Cyborg, "When Plantera has been defeated"),
				new TownNPCInfo(NPCID.SantaClaus, "When Frost Legion has been defeated, only during Christmas"),
				new TownNPCInfo(NPCID.Princess, "Have all other town npcs in the world"),
				new TownNPCInfo(NPCID.TownCat, $"Use [i:{ItemID.LicenseCat}]"),
				new TownNPCInfo(NPCID.TownDog, $"Use [i:{ItemID.LicenseDog}]"),
				new TownNPCInfo(NPCID.TownBunny, $"Use [i:{ItemID.LicenseBunny}]"),
			};

			foreach (ModNPC npc in ModContent.GetContent<ModNPC>()) {
				if (npc.NPC.townNPC && NPC.TypeToDefaultHeadIndex(npc.NPC.type) >= 0) // ignore traveling I guess.
				{
					//realTownNPCs.Add(npc.npc.type);
					var modSuppliedTownNPC = modTownNPCsInfos.FirstOrDefault(x => x.type == npc.NPC.type);
					if (modSuppliedTownNPC != null)
						realTownNPCsInfos.Add(modSuppliedTownNPC);
					else
						realTownNPCsInfos.Add(new TownNPCInfo(npc.NPC.type, "Conditions unknown"));
				}
			}

			//	ErrorLogger.Log(string.Join(", ", realTownNPCs));
			//	ErrorLogger.Log(string.Join(", ", realTownNPCs.Select(x => Lang.GetNPCNameValue(x))));
		}

		public override void Unload() {
			instance = null;
			CensusConfigClient.Instance = null;
			//townTracker = null;
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			var msgType = (CensusMessageType)reader.ReadByte();
			switch (msgType) {
				case CensusMessageType.CensusInfo:
					if (Main.netMode != NetmodeID.MultiplayerClient)
						return;
					WorldGen.prioritizedTownNPCType = reader.ReadInt32();
					int count = reader.ReadInt32();
					if (count != Main.townNPCCanSpawn.Length)
						Logger.Error("Census: Somehow Main.townNPCCanSpawn.Length incorrect");
					//var bitsBytes = BitsByte.DecomposeBitsBytesChain(reader);
					//for (int i = 0; i < bitsBytes.Length; i++) {
					//	BitsByte bitsByte = bitsBytes[i];
					//	for (int j = 0; j < 8 && j + i * 8 < Main.townNPCCanSpawn.Length; j++) {
					//		Main.townNPCCanSpawn[j + i * 8] = bitsByte[j];
					//	}
					//}
					for (int i = 0; i < Main.townNPCCanSpawn.Length; i += 8) {
						BitsByte bits = reader.ReadByte();
						for (int j = 0; j < 8 && j + i < Main.townNPCCanSpawn.Length; j++) {
							Main.townNPCCanSpawn[j + i] = bits[j];
						}
					}
					//for (int i = 0; i < Main.townNPCCanSpawn.Length; i++) {
					//	Main.townNPCCanSpawn[i] = reader.ReadBoolean();
					//}
					Census.calculated = true;
					break;
				default:
					Logger.Warn("Ceusus: Unknown Message type: " + msgType);
					break;
			}
		}

		/*
		public override void UpdateUI(GameTime gameTime)
		{
			if (Main.GameUpdateCount % 50 == 0)
			{
				if (Main.checkForSpawns < 7000)
				{
					Main.checkForSpawns = 7100;
				}

				//Main.NewText($"{nameof(Main.checkForSpawns)} {Main.checkForSpawns} -- {nameof(WorldGen.spawnDelay)} {WorldGen.spawnDelay}");
				Main.NewText($"{nameof(Main.checkForSpawns)} {Main.checkForSpawns}"); // once this is 7200
																					  //WorldGen.prioritizedTownNPC
																					  //Main.townNPCCanSpawn
																					  //Main.checkForSpawns
																					  //WorldGen.spawnDelay
			}

			if (Main.GameUpdateCount % 50 == 0)
			{
				Main.NewText($"{nameof(WorldGen.prioritizedTownNPC)} {WorldGen.prioritizedTownNPC}");

				Main.NewText(string.Join(", ", Main.townNPCCanSpawn.Select((x, i) => new { x, i }).Where(x => x.x).Select(x => Lang.GetNPCNameValue(x.i))));
			}

			if (WorldGen.prioritizedTownNPC > 0)
			{
				if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4) && !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4))
				{
					Point playerPoint = Main.LocalPlayer.Center.ToTileCoordinates();
					WorldGen.SpawnTownNPC(playerPoint.X, playerPoint.Y);
				}
			}
		}
		*/

		string hoverText = "";
		public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			if (Main.playerInventory && Main.EquipPage == 1) {
				int InventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
				if (InventoryIndex != -1) {
					layers.Insert(InventoryIndex + 1, new LegacyGameInterfaceLayer(
					"Census: Census Hover",
					delegate {
						if (!string.IsNullOrEmpty(hoverText)) {
							if (/*text != "" && */Main.mouseItem.type == ItemID.None) {
								//Main.instance.MouseText(text);
								//Main.HoverItem = new Item();
								//Main.hoverItemName = text;
								Vector2 vector = ChatManager.GetStringSize(FontAssets.MouseText.Value, hoverText, Vector2.One);
								//ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, Main.fontMouseText, text, new Vector2(12f, (float)Main.screenHeight - x) - stringSize * new Vector2(0f, 0f), Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, baseScale, -1f, y * 2f);


								int x = Main.mouseX + 10;
								int y = Main.mouseY + 10;
								if (Main.ThickMouse) {
									x += 6;
									y += 6;
								}
								if (x + vector.X + 4f > Main.screenWidth)
									x = (int)(Main.screenWidth - vector.X - 4f);
								if (y + vector.Y + 4f > Main.screenHeight)
									y = (int)(Main.screenHeight - vector.Y - 4f);
								Color baseColor = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
								ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, hoverText, new Vector2(x, y), baseColor, 0f, Vector2.Zero, Vector2.One, -1f, 2f);
							}
						}
						return true;
					},
					InterfaceScaleType.UI));


					layers.Insert(InventoryIndex, new LegacyGameInterfaceLayer(
					"Census: Census",
					delegate {
						// prioritizedTownNPC and townNPCCanSpawn not always up to date. townNPCCanSpawn needs to be set after 2 min in game during day. 
						bool unknown = false; // WorldGen.prioritizedTownNPC == 0 && Main.townNPCCanSpawn.All(x => !x);
						int total = UILinkPointNavigator.Shortcuts.NPCS_IconsTotal + 0;
						float oldInventoryScale = Main.inventoryScale;
						Main.inventoryScale = 0.85f;

						int mH = 0;
						if (Main.mapEnabled) {
							if (!Main.mapFullscreen && Main.mapStyle == 1)
								mH = 256;
							//if (Main.GameUpdateCount / 60 % 2 == 0)
							//	PlayerInput.SetZoom_UI();
							if (mH + Main.instance.RecommendedEquipmentAreaPushUp > Main.screenHeight)
								mH = Main.screenHeight - Main.instance.RecommendedEquipmentAreaPushUp;
						}

						// Order: vanilla check order, followed by mod npc order.

						/* From UpdateTime_SpawnTownNPCs:
							WorldGen.prioritizedTownNPC = 22;
							WorldGen.prioritizedTownNPC = 17;
							WorldGen.prioritizedTownNPC = 18;
							WorldGen.prioritizedTownNPC = 19;
							WorldGen.prioritizedTownNPC = 107;
							WorldGen.prioritizedTownNPC = 441;
							WorldGen.prioritizedTownNPC = 108;
							WorldGen.prioritizedTownNPC = 160;
							WorldGen.prioritizedTownNPC = 20;
							WorldGen.prioritizedTownNPC = 38;
							WorldGen.prioritizedTownNPC = 228;
							WorldGen.prioritizedTownNPC = 178;
							WorldGen.prioritizedTownNPC = 124;
							WorldGen.prioritizedTownNPC = 369;
							WorldGen.prioritizedTownNPC = 209;
							WorldGen.prioritizedTownNPC = 229;
							WorldGen.prioritizedTownNPC = 54;
							WorldGen.prioritizedTownNPC = 353;
							WorldGen.prioritizedTownNPC = 207;
							WorldGen.prioritizedTownNPC = 227;
							WorldGen.prioritizedTownNPC = 208;
							WorldGen.prioritizedTownNPC = 142;
							WorldGen.prioritizedTownNPC = 550;
						*/

						List<TownNPCInfo> canSpawns = new List<TownNPCInfo>();
						List<TownNPCInfo> cantSpawns = new List<TownNPCInfo>();
						//for (int i = 1; i < Main.npcHeadTexture.Length; i++)
						//{
						//	if (i == 21)
						//		continue;
						//	int missingNPCType = NPC.HeadIndexToType(i);

						foreach (TownNPCInfo townNPCInfo in Census.instance.realTownNPCsInfos)
						//foreach (var missingNPCType in realTownNPCs)
						{
							bool missing = !NPC.AnyNPCs(townNPCInfo.type);
							if (missing) {
								if (WorldGen.prioritizedTownNPCType == townNPCInfo.type)
									canSpawns.Insert(0, townNPCInfo);
								else if (Main.townNPCCanSpawn[townNPCInfo.type])
									canSpawns.Add(townNPCInfo);
								else
									cantSpawns.Add(townNPCInfo);
							}
						}

						//int[] o = new int[2];
						//canSpawns.Sort((x, y) => o.i  x < y ? 1 : -1);

						int drawCount = 0;
						string text = "";

						//int space = Main.screenHeight - 80 - (int)(174 + mH + drawCount * 56 * Main.inventoryScale);
						//int perRow = space / (int)(56 * Main.inventoryScale) + 1;
						//int startRow = total % perRow;
						//int startCol = total / perRow;

						int perColumn = UILinkPointNavigator.Shortcuts.NPCS_IconsPerColumn;
						int startRow = total / perColumn;
						int startCol = total % perColumn;

						//Main.NewText(perRow);
						int rowOffsetY = (int)(startRow * 56 * Main.inventoryScale);
						int colOffsetX = startCol * -48;

						//foreach (var missingNPCType in canSpawns.Concat(cantSpawns))
						//foreach ((int missingNPCType, string condition) in realTownNPCsInfos)
						//for (int i = 1; i < Main.npcHeadTexture.Length; i++)

						//c#7		//foreach ((int missingNPCType, string condition) in canSpawns.Concat(cantSpawns))
						foreach (TownNPCInfo t in canSpawns.Concat(cantSpawns)) {
							int missingNPCType = t.type;
							string condition = t.conditions;

							//if (i == 21)
							//	continue;

							int i = NPC.TypeToDefaultHeadIndex(missingNPCType);

							//int missingNPCType = NPC.HeadIndexToType(i);
							//int missingNPCWhoAmI = 0;
							//bool missing = true;
							//for (int j = 0; j < 200; j++)
							//{
							//	if (Main.npc[j].active && Main.npc[j].type == missingNPCType /*NPC.TypeToHeadIndex(Main.npc[j].type) == i*/)
							//	{
							//		missing = false;
							//		missingNPCWhoAmI = j;
							//		break;
							//	}
							//}
							//if (missing)
							{
								int drawX = Main.screenWidth - 64 - 28 + colOffsetX;
								int drawY = (int)(174 + mH + drawCount * 56 * Main.inventoryScale) + rowOffsetY;
								Color white = new Color(100, 100, 100, 100);
								if (drawY > Main.screenHeight - 80) {
									colOffsetX -= 48;
									rowOffsetY -= drawY - (174 + mH);
									drawX = Main.screenWidth - 64 - 28 + colOffsetX;
									drawY = (int)(174 + mH + drawCount * 56 * Main.inventoryScale) + rowOffsetY;
								}
								if (Main.mouseX >= drawX && Main.mouseX <= drawX + TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= drawY && Main.mouseY <= drawY + TextureAssets.InventoryBack.Height() * Main.inventoryScale) {
									Main.mouseText = true;
									//text = Main.npc[missingNPCWhoAmI].FullName;
									text = Lang.GetNPCNameValue(missingNPCType);
									if (WorldGen.prioritizedTownNPCType == missingNPCType)
										text += "\nNext";
									else if (Main.townNPCCanSpawn[missingNPCType]) {
										//text += "\nOn their way!";
									}
									else {
										if (unknown)
											text += $" - Townspeople spawn during the day";
										else
											text += $" - {condition}";
										//	text += "\nNot coming" + $"\nNeeds: {condition}";
									}
								}
								//Texture2D texture = Main.inventoryBack6Texture; // 6, 5
								//if (!Main.townNPCCanSpawn[missingNPCType])
								//	texture = Main.inventoryBack5Texture;
								//if (WorldGen.prioritizedTownNPC == missingNPCType)
								//	texture = Main.inventoryBack7Texture;
								Texture2D texture = TextureAssets.InventoryBack7.Value;
								Color white2 = Main.inventoryBack;
								Main.spriteBatch.Draw(texture, new Vector2(drawX, drawY), null, white2, 0f, default(Vector2), Main.inventoryScale, SpriteEffects.None, 0f);
								white = Color.White;
								float scale = 1f;
								float maxDimension = Math.Min(TextureAssets.NpcHead[i].Width(), TextureAssets.NpcHead[i].Height());
								if (maxDimension > 36f)
									scale = 36f / maxDimension;
								Main.spriteBatch.Draw(TextureAssets.NpcHead[i].Value, new Vector2(drawX + 26f * Main.inventoryScale, drawY + 26f * Main.inventoryScale), null, white, 0f, new Vector2(TextureAssets.NpcHead[i].Width() / 2, (float)(TextureAssets.NpcHead[i].Height() / 2)), scale, SpriteEffects.None, 0f);

								ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.ItemStack.Value, !Census.calculated ? "?" : Main.townNPCCanSpawn[missingNPCType] ? "✓" : "X", new Vector2(drawX + 26f * Main.inventoryScale, drawY + 26f * Main.inventoryScale) + new Vector2(6f, 6f), !Census.calculated ? Color.MediumPurple : Main.townNPCCanSpawn[missingNPCType] ? Color.LightGreen : Color.LightSalmon, 0f, Vector2.Zero, new Vector2(0.7f));

								drawCount++;
							}
						}
						hoverText = text;

						if (UILinkPointNavigator.Shortcuts.NPCS_LastHovered > -1 && CensusConfigClient.Instance.ShowLocatingArrow) {
							//Main.NewText("" + Main.npc[UILinkPointNavigator.Shortcuts.NPCS_LastHovered].Center.X);
							var npc = Main.npc[UILinkPointNavigator.Shortcuts.NPCS_LastHovered];
							var headIndex = NPC.TypeToDefaultHeadIndex(npc.type); // if NPCS_LastHovered is 0, it could also be the housing query button.
							Vector2 playerCenter = Main.LocalPlayer.Center + new Vector2(0, Main.LocalPlayer.gfxOffY);
							var vector = npc.Center - playerCenter;
							var distance = vector.Length();
							if (headIndex > -1 && distance > 40) {
								var headTexture = TextureAssets.NpcHead[headIndex].Value;
								var offset = Vector2.Normalize(vector) * Math.Min(70, distance - 20);
								float rotation = vector.ToRotation() + (float)(3 * Math.PI / 4);
								var drawPosition = playerCenter - Main.screenPosition + offset;
								float fade = Math.Min(1f, (distance - 20) / 70);
								Main.spriteBatch.Draw(TextureAssets.Cursors[0].Value, drawPosition, null, CensusConfigClient.Instance.ArrowColor * fade, rotation, TextureAssets.Cursors[1].Value.Size() / 2, new Vector2(1.5f), SpriteEffects.None, 0);

								drawPosition -= Vector2.Normalize(vector) * 20;

								Main.spriteBatch.Draw(headTexture, drawPosition, null, Color.White * fade, 0, headTexture.Size() / 2, Vector2.One, npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
							}
						}

						Main.inventoryScale = oldInventoryScale;
						return true;
					},
					InterfaceScaleType.UI)
				);
				}
			}
		}

		// string:"TownNPCCondition" - int:npcid - string:condition
		public override object Call(params object[] args)
		{
			try
			{
				// Where should other mods call? They could call at end of Load?
				string message = args[0] as string;
				if (message == "TownNPCCondition")
				{
					int type = Convert.ToInt32(args[1]);
					string condition = args[2] as string; // when are lang files ready?
					modTownNPCsInfos.Add(new TownNPCInfo(type, condition));
					return "Success";
				}
				else
				{
					Logger.Error("Census Call Error: Unknown Message: " + message);
				}
			}
			catch (Exception e)
			{
				Logger.Error("Census Call Error: " + e.StackTrace + e.Message);
			}
			return "Failure";
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

