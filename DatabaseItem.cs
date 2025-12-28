using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;

namespace Ratopia;

[HarmonyPatch(typeof(DB_Mgr), "Item_DB_Setting")]
public class DatabaseItem
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
					Vars.LogForHarmony.LogWarning($"[Item] Patch Failed: item {entry.Name} not found");
					continue;
				}

				if (item.Enable == 0)
				{
					Vars.LogForHarmony.LogInfo($"[Item] Patch Skipped: item {entry.Name} not enabled");
					continue;
				}

				if (item.Enable == entry.Enable && item.Recipe == entry.Recipe && item.Ability == entry.Ability)
				{
					Vars.LogForHarmony.LogInfo(
						$"[Item] Patch Skipped: item [{entry.Name}] not changed"
					);
					continue;
				}

				Vars.LogForHarmony.LogInfo(
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

	[HarmonyPrefix]
	private static void Prefix(DB_Mgr __instance)
	{
		Vars.LogForHarmony.LogInfo("[Item] Patching...");

		var db = __instance.m_Item_DB1;
		if (db == null)
		{
			Vars.LogForHarmony.LogError("[Item] __instance.m_Item_DB1 == null");
			return;
		}

		if (!File.Exists(Vars.FilePathItem))
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

				File.WriteAllText(Vars.FilePathItem, jsonContent);
				Vars.LogForHarmony.LogInfo("[Item] Bump File Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Item] Bump File Failed: {e}");
			}
		}
		else
		{
			try
			{
				var jsonContent = File.ReadAllText(Vars.FilePathItem);
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

				Vars.LogForHarmony.LogInfo("[Item] Patch Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Item] Patch Failed: {e}");
			}
		}
	}
}