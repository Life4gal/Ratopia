using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[BepInPlugin("Ratopia", "Ratopia", "1.0.0")]
public class RatopiaPlugin : BaseUnityPlugin
{
	private static ManualLogSource LogForHarmony;

	private static readonly string PluginFolder = Path.Combine(Paths.PluginPath, "Ratopia");

	private static readonly string FilePathQueenCharacter = Path.Combine(PluginFolder, "QueenCharacter.json");
	private static readonly string FilePathProsperity = Path.Combine(PluginFolder, "Prosperity.json");

	private static readonly string FilePathResource = Path.Combine(PluginFolder, "Resource.json");

	// private static readonly string FilePathPlant = Path.Combine(PluginFolder, "Plant.json");
	private static readonly string FilePathBuilding = Path.Combine(PluginFolder, "Building.json");

	private static readonly string FilePathItem = Path.Combine(PluginFolder, "Item.json");
	// private static readonly string FilePathRatron = Path.Combine(PluginFolder, "Ratron.json");
	// private static readonly string FilePathRecipe = Path.Combine(PluginFolder, "Recipe.json");

	private void Awake()
	{
		if (!Directory.Exists(PluginFolder))
		{
			Directory.CreateDirectory(PluginFolder);
		}

		var harmony = new Harmony("Ratopia");
		harmony.PatchAll();
	}

	private void OnEnable()
	{
		LogForHarmony = Logger;
	}

	[HarmonyPatch(typeof(DB_Mgr), "QueenCharacter_DB_Setting")]
	private class PatchQueenCharacter
	{
		private class Entry
		{
			public string Name;
			public string Ability;
		}

		private static void Prefix(DB_Mgr __instance)
		{
			LogForHarmony.LogInfo("[QueenCharacter] Patching...");

			var db = __instance.m_QueenCharacter_DB;
			if (db == null)
			{
				LogForHarmony.LogError("[QueenCharacter] __instance.m_QueenCharacter_DB == null");
				return;
			}

			if (!File.Exists(FilePathQueenCharacter))
			{
				try
				{
					var sheet = db.sheets[0];

					var fileContent = sheet.list
						.Where(item => item.Enable != 0)
						.Select(item => new Entry
						{
							Name = item.Name,
							Ability = item.Ability,
						})
						.ToList();
					var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

					File.WriteAllText(FilePathQueenCharacter, jsonContent);
					LogForHarmony.LogInfo("[QueenCharacter] Bump File Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[QueenCharacter] Bump File Failed: {e}");
				}
			}
			else
			{
				try
				{
					var jsonContent = File.ReadAllText(FilePathQueenCharacter);
					var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

					var sheet = db.sheets[0];

					foreach (var entry in fileContent)
					{
						var item = sheet.list.Find(item => item.Name == entry.Name);
						if (item == null)
						{
							LogForHarmony.LogWarning($"[QueenCharacter] Patch Failed: item {entry.Name} not found");
							continue;
						}

						if (item.Enable == 0)
						{
							LogForHarmony.LogInfo($"[QueenCharacter] Patch Skipped: item {entry.Name} not enabled");
							continue;
						}

						if (item.Ability == entry.Ability)
						{
							LogForHarmony.LogInfo(
								$"[QueenCharacter] Patch Skipped: item [{entry.Name}] not changed({entry.Ability})"
							);
							continue;
						}

						LogForHarmony.LogInfo(
							$"[QueenCharacter]\n" +
							$"{entry.Name}: " +
							$"\n\tAbility: [{item.Ability}] ==> [{entry.Ability}]"
						);

						item.Ability = entry.Ability;
					}

					LogForHarmony.LogInfo("[QueenCharacter] Patch Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[QueenCharacter] Patch Failed: {e}");
				}
			}
		}
	}

	[HarmonyPatch(typeof(DB_Mgr), "Prosperity_DB_Setting")]
	private class PatchProsperity
	{
		private class Entry
		{
			public string Name;
			public int NeedValue;
			public int Pop;
			public int CitizenAbilityValue;
			public int PolicyNum;
		}

		private static void Prefix(DB_Mgr __instance)
		{
			LogForHarmony.LogInfo("[Prosperity] Patching...");

			var db = __instance.m_Prosperity_DB1;
			if (db == null)
			{
				LogForHarmony.LogError("[Prosperity] __instance.m_Item_DB1 == null");
				return;
			}

			if (!File.Exists(FilePathProsperity))
			{
				try
				{
					var sheet = db.sheets[0];

					var fileContent = sheet.list
						.Select(item => new Entry
						{
							Name = item.Name,
							NeedValue = item.NeedValue,
							Pop = item.Pop,
							CitizenAbilityValue = item.CitizenAbilityValue,
							PolicyNum = item.PolicyNum,
						})
						.ToList();
					var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

					File.WriteAllText(FilePathProsperity, jsonContent);
					LogForHarmony.LogInfo("[Prosperity] Bump File Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Prosperity] Bump File Failed: {e}");
				}
			}
			else
			{
				try
				{
					var jsonContent = File.ReadAllText(FilePathProsperity);
					var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

					var sheet = db.sheets[0];

					foreach (var entry in fileContent)
					{
						var item = sheet.list.Find(item => item.Name == entry.Name);
						if (item == null)
						{
							LogForHarmony.LogWarning($"[Prosperity] Patch Failed: item {entry.Name} not found");
							continue;
						}

						if (
							item.NeedValue == entry.NeedValue &&
							item.Pop == entry.PolicyNum &&
							item.CitizenAbilityValue == entry.CitizenAbilityValue &&
							item.PolicyNum == entry.PolicyNum
						)
						{
							LogForHarmony.LogInfo(
								$"[Prosperity] Patch Skipped: item [{entry.Name}] not changed"
							);
							continue;
						}

						LogForHarmony.LogInfo(
							$"[Prosperity]\n" +
							$"{entry.Name}: " +
							$"\n\tNeedValue: [{item.NeedValue}] ==> [{entry.NeedValue}]" +
							$"\n\tPop: [{item.Pop}] ==> [{entry.Pop}]" +
							$"\n\tCitizenAbilityValue: [{item.CitizenAbilityValue}] ==> [{entry.CitizenAbilityValue}]" +
							$"\n\tPolicyNum: [{item.PolicyNum}] ==> [{entry.PolicyNum}]"
						);

						item.NeedValue = entry.NeedValue;
						item.Pop = entry.Pop;
						item.CitizenAbilityValue = entry.CitizenAbilityValue;
						item.PolicyNum = entry.PolicyNum;
					}

					LogForHarmony.LogInfo("[Prosperity] Patch Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Prosperity] Patch Failed: {e}");
				}
			}
		}
	}

	[HarmonyPatch(typeof(DB_Mgr), "Res_DB_Setting")]
	private class PatchResource
	{
		private class Entry
		{
			public class Recipe
			{
				public string Material;
				public int Quantity;
				public int Workload;
			}

			public string Name;
			public string Ability;
			public string Mine;

			public Recipe RecipeA;
			public Recipe RecipeB;
			public Recipe RecipeC;
		}

		private static bool IsUsableResource(Res_DB1.Param item)
		{
			return item.Category is 1 or 2;
		}

		private static string AbilityOfResource(Res_DB1.Param item)
		{
			return IsUsableResource(item)
				? item.Ability
				: "This resource is unusable, so no ability can be provided.(Any settings will be ignored)";
		}

		private static void SetAbilityOfResource(Res_DB1.Param item, string ability)
		{
			if (IsUsableResource(item))
			{
				item.Ability = ability;
			}
		}

		private static bool IsMinableResource(Res_DB1.Param item)
		{
			return item.Category == 0;
		}

		private static string MineOfResource(Res_DB1.Param item)
		{
			return IsMinableResource(item)
				? item.Mine
				: "This resource is unminable.(Any settings will be ignored)";
		}

		private static void SetMineOfResource(Res_DB1.Param item, string mine)
		{
			if (IsMinableResource(item))
			{
				item.Mine = mine;
			}
		}

		private static void Prefix(DB_Mgr __instance)
		{
			LogForHarmony.LogInfo("[Resource] Patching...");

			var db = __instance.m_Res_DB1;
			if (db == null)
			{
				LogForHarmony.LogError("[Resource] __instance.m_Res_DB1 == null");
				return;
			}

			if (!File.Exists(FilePathResource))
			{
				try
				{
					var sheet = db.sheets[0];

					var fileContent = sheet.list
						.Where(item => item.Enable != 0)
						.Select(item => new Entry
						{
							Name = item.Name,
							Ability = AbilityOfResource(item),
							Mine = MineOfResource(item),
							RecipeA = new Entry.Recipe
								{ Material = item.Material_A, Quantity = item.Product_A, Workload = item.BP_A },
							RecipeB = new Entry.Recipe
								{ Material = item.Material_B, Quantity = item.Product_B, Workload = item.BP_A },
							RecipeC = new Entry.Recipe
								{ Material = item.Material_C, Quantity = item.Product_C, Workload = item.BP_C }
						})
						.ToList();
					var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

					File.WriteAllText(FilePathResource, jsonContent);
					LogForHarmony.LogInfo("[Resource] Bump File Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Resource] Bump File Failed: {e}");
				}
			}
			else
			{
				try
				{
					var jsonContent = File.ReadAllText(FilePathResource);
					var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

					var sheet = db.sheets[0];

					foreach (var entry in fileContent)
					{
						var item = sheet.list.Find(item => item.Name == entry.Name);
						if (item == null)
						{
							LogForHarmony.LogWarning($"[Resource] Patch Failed: item {entry.Name} not found");
							continue;
						}

						if (item.Enable == 0)
						{
							LogForHarmony.LogInfo($"[Resource] Patch Skipped: item {entry.Name} not enabled");
							continue;
						}

						if (
							(!IsUsableResource(item) || AbilityOfResource(item) == entry.Ability) &&
							(!IsMinableResource(item) || MineOfResource(item) == entry.Mine) &&
							item.Material_A == entry.RecipeA.Material &&
							item.Product_A == entry.RecipeA.Quantity &&
							item.BP_A == entry.RecipeA.Workload &&
							item.Material_B == entry.RecipeB.Material &&
							item.Product_B == entry.RecipeB.Quantity &&
							item.BP_B == entry.RecipeB.Workload &&
							item.Material_C == entry.RecipeC.Material &&
							item.Product_C == entry.RecipeC.Quantity &&
							item.BP_C == entry.RecipeC.Workload
						)
						{
							LogForHarmony.LogInfo(
								$"[Resource] Patch Skipped: item [{entry.Name}] not changed"
							);
							continue;
						}

						var log =
							$"[Resource]\n" +
							$"{entry.Name}: ";
						if (IsUsableResource(item))
						{
							log += $"\n\tAbility: [{AbilityOfResource(item)}] ==> [{entry.Ability}]";
						}

						if (IsMinableResource(item))
						{
							log += $"\n\tMine: [{MineOfResource(item)}] ==> [{entry.Mine}]";
						}

						log +=
							$"\n\tRecipeA: " +
							$"Material: [{item.Material_A}] ==> [{entry.RecipeA.Material}], " +
							$"Quantity: [{item.Product_A}] ==> [{entry.RecipeA.Quantity}], " +
							$"Workload: [{item.BP_A}] ==> [{entry.RecipeA.Workload}]";
						log +=
							$"\n\tRecipeB: " +
							$"Material: [{item.Material_B}] ==> [{entry.RecipeB.Material}], " +
							$"Quantity: [{item.Product_B}] ==> [{entry.RecipeB.Quantity}], " +
							$"Workload: [{item.BP_B}] ==> [{entry.RecipeB.Workload}]";
						log +=
							$"\n\tRecipeA: " +
							$"Material: [{item.Material_C}] ==> [{entry.RecipeC.Material}], " +
							$"Quantity: [{item.Product_C}] ==> [{entry.RecipeC.Quantity}], " +
							$"Workload: [{item.BP_C}] ==> [{entry.RecipeC.Workload}]";

						LogForHarmony.LogInfo(log);

						SetAbilityOfResource(item, entry.Ability);
						SetMineOfResource(item, entry.Mine);
						item.Material_A = entry.RecipeA.Material;
						item.Product_A = entry.RecipeA.Quantity;
						item.BP_A = entry.RecipeA.Workload;
						item.Material_B = entry.RecipeB.Material;
						item.Product_B = entry.RecipeB.Quantity;
						item.BP_B = entry.RecipeB.Workload;
						item.Material_C = entry.RecipeC.Material;
						item.Product_C = entry.RecipeC.Quantity;
						item.BP_C = entry.RecipeC.Workload;

						LogForHarmony.LogInfo("[Resource] Patch Succeed");
					}
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Resource] Patch Failed: {e}");
				}
			}
		}
	}

	// [HarmonyPatch(typeof(DB_Mgr), "Plant_DB_Setting")]
	// private class PatchPlant
	// {
	// 	private static void Prefix(DB_Mgr __instance)
	// 	{
	// 		LogForHarmony.LogInfo("PatchPlant...");
	//
	// 		var db = __instance.m_Plant_DB1;
	// 		if (db == null)
	// 		{
	// 			LogForHarmony.LogError("__instance.m_Plant_DB1 == null");
	// 			return;
	// 		}
	//
	// 		if (!File.Exists(FilePathPlant))
	// 		{
	// 			try
	// 			{
	// 				var sheet = db.sheets[0];
	// 				var list = sheet.list;
	// 				var content = JsonConvert.SerializeObject(list, Formatting.Indented);
	//
	// 				File.WriteAllText(FilePathPlant, content);
	// 				LogForHarmony.LogInfo("Write Plant Succeed");
	// 			}
	// 			catch (Exception e)
	// 			{
	// 				LogForHarmony.LogError($"Write Plant Failed: {e}");
	// 			}
	// 		}
	// 		else
	// 		{
	// 			//
	// 		}
	// 	}
	// }

	[HarmonyPatch(typeof(DB_Mgr), "Build_DB_Setting")]
	private class PatchBuilding
	{
		private class Entry
		{
			public string Name;
			public int Width;
			public int Height;
			public int HP;
			public int Cost;
			public int ElecCost;
			public int Payment;
			public string Material;
			public string Product;
		}

		private static bool IsProductionBuilding(Building_DB1.Param item)
		{
			return item.Category == 3;
		}

		private static string ProductOfBuilding(Building_DB1.Param item)
		{
			return IsProductionBuilding(item)
				? item.Effect_Value3
				: "This building is not a production facility, so it cannot create products.(Any settings will be ignored)";
		}

		private static void SetProductOfBuilding(Building_DB1.Param item, string production)
		{
			if (IsProductionBuilding(item))
			{
				item.Effect_Value3 = production;
			}
		}

		private static void Prefix(DB_Mgr __instance)
		{
			LogForHarmony.LogInfo("[Building] Patching...");

			var db = __instance.m_Building_DB1;
			if (db == null)
			{
				LogForHarmony.LogError("[Building] __instance.m_Building_DB1 == null");
				return;
			}

			if (!File.Exists(FilePathBuilding))
			{
				try
				{
					var sheet = db.sheets[0];

					var fileContent = sheet.list
						.Where(item => item.Enable != 0)
						.Select(item => new Entry
						{
							Name = item.Name,
							Width = item.Width,
							Height = item.Height,
							HP = item.HP,
							Cost = item.Cost,
							ElecCost = item.ElecCost,
							Payment = item.Payment,
							Material = item.Material,
							Product = ProductOfBuilding(item)
						})
						.ToList();
					var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

					File.WriteAllText(FilePathBuilding, jsonContent);
					LogForHarmony.LogInfo("[Building] Bump File Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Building] Bump File Failed: {e}");
				}
			}
			else
			{
				try
				{
					var jsonContent = File.ReadAllText(FilePathBuilding);
					var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

					var sheet = db.sheets[0];

					foreach (var entry in fileContent)
					{
						var item = sheet.list.Find(item => item.Name == entry.Name);
						if (item == null)
						{
							LogForHarmony.LogWarning($"[Building] Patch Failed: item {entry.Name} not found");
							continue;
						}

						if (item.Enable == 0)
						{
							LogForHarmony.LogInfo($"[Building] Patch Skipped: item {entry.Name} not enabled");
							continue;
						}

						if (
							item.Width == entry.Width &&
							item.Height == entry.Height &&
							item.HP == entry.HP &&
							item.Cost == entry.Cost &&
							item.ElecCost == entry.ElecCost &&
							item.Payment == entry.Payment &&
							item.Material == entry.Material &&
							(!IsProductionBuilding(item) || ProductOfBuilding(item) == entry.Product)
						)
						{
							LogForHarmony.LogInfo(
								$"[Building] Patch Skipped: item [{entry.Name}] not changed"
							);
							continue;
						}

						var log =
							$"[Building]\n" +
							$"{entry.Name}: " +
							$"\n\tWidth: [{item.Width}] ==> [{entry.Width}]" +
							$"\n\tHeight: [{item.Height}] ==> [{entry.Height}]" +
							$"\n\tHP: [{item.HP}] ==> [{entry.HP}]" +
							$"\n\tCost: [{item.Cost}] ==> [{entry.Cost}]" +
							$"\n\tElecCost: [{item.ElecCost}] ==> [{entry.ElecCost}]" +
							$"\n\tPayment: [{item.Payment}] ==> [{entry.Payment}]" +
							$"\n\tMaterial: [{item.Material}] ==> [{entry.Material}]";
						if (IsProductionBuilding(item))
						{
							log += $"\n\tProduct: [{ProductOfBuilding(item)}] ==> [{entry.Product}]";
						}

						LogForHarmony.LogInfo(log);

						item.Width = entry.Width;
						item.Height = entry.Height;
						item.HP = entry.HP;
						item.Cost = entry.Cost;
						item.ElecCost = entry.ElecCost;
						item.Payment = entry.Payment;
						item.Material = entry.Material;
						SetProductOfBuilding(item, entry.Product);
					}

					LogForHarmony.LogInfo("[Building] Patch Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Building] Patch Failed: {e}");
				}
			}
		}
	}

	// Config/Item.txt
	[HarmonyPatch(typeof(DB_Mgr), "Item_DB_Setting")]
	private class PatchItem
	{
		private class Entry
		{
			// 0 == disable
			// 1 == enable
			// others == unlock when conditions are met
			public int Enable;
			public string Name;
			public string Recipe;
			public string Ability;
		}

		private class FileContent
		{
			public List<Entry> Weapon;
			public List<Entry> Clothes;
			public List<Entry> Accessory;

			public static List<Entry> FilterEntries(List<Item_DB1.Param> items)
			{
				return items
					.Where(item => item.Enable != 0)
					.Select(item => new Entry
					{
						Enable = item.Enable,
						Name = item.Name,
						Recipe = item.Recipe,
						Ability = item.Ability
					})
					.ToList();
			}

			public static void ApplyEntries(List<Item_DB1.Param> items, List<Entry> entries)
			{
				foreach (var entry in entries)
				{
					var item = items.Find(item => item.Name == entry.Name);
					if (item == null)
					{
						LogForHarmony.LogWarning($"[Item] Patch Failed: item {entry.Name} not found");
						continue;
					}

					if (item.Enable == 0)
					{
						LogForHarmony.LogInfo($"[Item] Patch Skipped: item {entry.Name} not enabled");
						continue;
					}

					if (item.Enable == entry.Enable && item.Recipe == entry.Recipe && item.Ability == entry.Ability)
					{
						LogForHarmony.LogInfo(
							$"[Item] Patch Skipped: item [{entry.Name}] not changed"
						);
						continue;
					}

					LogForHarmony.LogInfo(
						$"[Item]\n" +
						$"{entry.Name}: " +
						$"\n\tEnable: [{item.Enable}] ==> [{entry.Enable}]" +
						$"\n\tRecipe: [{item.Recipe}] ==> [{entry.Recipe}]" +
						$"\n\tAbility: [{item.Ability}] ==> [{entry.Ability}]"
					);

					item.Enable = entry.Enable;
					item.Recipe = entry.Recipe;
					item.Ability = entry.Ability;
				}
			}
		}

		private static void Prefix(DB_Mgr __instance)
		{
			LogForHarmony.LogInfo("[Item] Patching...");

			var db = __instance.m_Item_DB1;
			if (db == null)
			{
				LogForHarmony.LogError("[Item] __instance.m_Item_DB1 == null");
				return;
			}

			if (!File.Exists(FilePathItem))
			{
				try
				{
					var weaponList = db.sheets[0].list;
					var clothList = db.sheets[1].list;
					var accessoryList = db.sheets[2].list;

					var weapon = FileContent.FilterEntries(weaponList);
					var cloth = FileContent.FilterEntries(clothList);
					var accessory = FileContent.FilterEntries(accessoryList);

					var fileContent = new FileContent { Weapon = weapon, Clothes = cloth, Accessory = accessory };
					var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

					File.WriteAllText(FilePathItem, jsonContent);
					LogForHarmony.LogInfo("[Item] Bump File Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Item] Bump File Failed: {e}");
				}
			}
			else
			{
				try
				{
					var jsonContent = File.ReadAllText(FilePathItem);
					var fileContent = JsonConvert.DeserializeObject<FileContent>(jsonContent);

					var weapon = fileContent.Weapon;
					var cloth = fileContent.Clothes;
					var accessory = fileContent.Accessory;

					var weaponList = db.sheets[0].list;
					var clothList = db.sheets[1].list;
					var accessoryList = db.sheets[2].list;

					FileContent.ApplyEntries(weaponList, weapon);
					FileContent.ApplyEntries(clothList, cloth);
					FileContent.ApplyEntries(accessoryList, accessory);

					LogForHarmony.LogInfo("[Item] Patch Succeed");
				}
				catch (Exception e)
				{
					LogForHarmony.LogError($"[Item] Patch Failed: {e}");
				}
			}
		}
	}

	// [HarmonyPatch(typeof(DB_Mgr), "Ratron_DB_Setting")]
	// private class PatchRatron
	// {
	// 	private static void Prefix(DB_Mgr __instance)
	// 	{
	// 		LogForHarmony.LogInfo("PatchRatron...");
	//
	// 		var db = __instance.m_Ratron_DB1;
	// 		if (db == null)
	// 		{
	// 			LogForHarmony.LogError("__instance.m_Ratron_DB1 == null");
	// 			return;
	// 		}
	//
	// 		if (!File.Exists(FilePathRatron))
	// 		{
	// 			try
	// 			{
	// 				var head = db.sheets[0];
	// 				var body = db.sheets[1];
	// 				var parts = db.sheets[2];
	// 				var data = new { head, body, parts, };
	// 				var content = JsonConvert.SerializeObject(data, Formatting.Indented);
	//
	// 				File.WriteAllText(FilePathRatron, content);
	// 				LogForHarmony.LogInfo("Write Ratron Succeed");
	// 			}
	// 			catch (Exception e)
	// 			{
	// 				LogForHarmony.LogError($"Write Ratron Failed: {e}");
	// 			}
	// 		}
	// 		else
	// 		{
	// 			//
	// 		}
	// 	}
	// }

	// [HarmonyPatch(typeof(DB_Mgr), "Recipe_DB_Setting")]
	// private class PatchRecipe
	// {
	// 	private static void Prefix(DB_Mgr __instance)
	// 	{
	// 		LogForHarmony.LogInfo("PatchRecipe...");
	//
	// 		var db = __instance.m_RecipeDB;
	// 		if (db == null)
	// 		{
	// 			LogForHarmony.LogError("__instance.m_RecipeDB == null");
	// 			return;
	// 		}
	//
	// 		if (!File.Exists(FilePathRecipe))
	// 		{
	// 			try
	// 			{
	// 				var list = db._list;
	// 				var food1 = db.List_Food1;
	// 				var food2 = db.List_Food1;
	// 				var food3 = db.List_Food1;
	// 				var data = new { list, food1, food2, food3, };
	// 				var content = JsonConvert.SerializeObject(data, Formatting.Indented);
	//
	// 				File.WriteAllText(FilePathRecipe, content);
	// 				LogForHarmony.LogInfo("Write Recipe Succeed");
	// 			}
	// 			catch (Exception e)
	// 			{
	// 				LogForHarmony.LogError($"Write Recipe Failed: {e}");
	// 			}
	// 		}
	// 		else
	// 		{
	// 			//
	// 		}
	// 	}
	// }
}