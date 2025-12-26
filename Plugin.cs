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

	// private static readonly string FilePathResource = Path.Combine(PluginFolder, "Resource.json");
	// private static readonly string FilePathPlant = Path.Combine(PluginFolder, "Plant.json");
	// private static readonly string FilePathBuilding = Path.Combine(PluginFolder, "Building.json");
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

	// [HarmonyPatch(typeof(DB_Mgr), "Res_DB_Setting")]
	// private class PatchResource
	// {
	// 	private static void Prefix(DB_Mgr __instance)
	// 	{
	// 		LogForHarmony.LogInfo("PatchResource...");
	//
	// 		var db = __instance.m_Res_DB1;
	// 		if (db == null)
	// 		{
	// 			LogForHarmony.LogError("__instance.m_Res_DB1 == null");
	// 			return;
	// 		}
	//
	// 		if (!File.Exists(FilePathResource))
	// 		{
	// 			try
	// 			{
	// 				var sheet = db.sheets[0];
	// 				var list = sheet.list;
	// 				var content = JsonConvert.SerializeObject(list, Formatting.Indented);
	//
	// 				File.WriteAllText(FilePathResource, content);
	// 				LogForHarmony.LogInfo("Write Resource Succeed");
	// 			}
	// 			catch (Exception e)
	// 			{
	// 				LogForHarmony.LogError($"Write Resource Failed: {e}");
	// 			}
	// 		}
	// 		else
	// 		{
	// 			//
	// 		}
	// 	}
	// }

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

	// [HarmonyPatch(typeof(DB_Mgr), "Build_DB_Setting")]
	// private class PatchBuilding
	// {
	// 	private static void Prefix(DB_Mgr __instance)
	// 	{
	// 		LogForHarmony.LogInfo("PatchBuilding...");
	//
	// 		var db = __instance.m_Building_DB1;
	// 		if (db == null)
	// 		{
	// 			LogForHarmony.LogError("__instance.m_Building_DB1 == null");
	// 			return;
	// 		}
	//
	// 		if (!File.Exists(FilePathBuilding))
	// 		{
	// 			try
	// 			{
	// 				var sheet = db.sheets[0];
	// 				var list = sheet.list;
	// 				var content = JsonConvert.SerializeObject(list, Formatting.Indented);
	//
	// 				File.WriteAllText(FilePathBuilding, content);
	// 				LogForHarmony.LogInfo("Write Building Succeed");
	// 			}
	// 			catch (Exception e)
	// 			{
	// 				LogForHarmony.LogError($"Write Building Failed: {e}");
	// 			}
	// 		}
	// 		else
	// 		{
	// 			//
	// 		}
	// 	}
	// }

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