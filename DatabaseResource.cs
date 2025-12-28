using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;

namespace Ratopia;

[HarmonyPatch(typeof(DB_Mgr), "Res_DB_Setting")]
public class DatabaseResource
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

	private static bool IsUsableResource(in Res_DB1.Param item)
	{
		return item.Category is 1 or 2;
	}

	private static string AbilityOfResource(in Res_DB1.Param item)
	{
		return IsUsableResource(item)
			? item.Ability
			: "This resource is unusable, so no ability can be provided.(Any settings will be ignored)";
	}

	private static void SetAbilityOfResource(ref Res_DB1.Param item, string ability)
	{
		if (IsUsableResource(item))
		{
			item.Ability = ability;
		}
	}

	private static bool IsMinableResource(in Res_DB1.Param item)
	{
		return item.Category == 0;
	}

	private static string MineOfResource(in Res_DB1.Param item)
	{
		return IsMinableResource(item)
			? item.Mine
			: "This resource is unminable.(Any settings will be ignored)";
	}

	private static void SetMineOfResource(ref Res_DB1.Param item, string mine)
	{
		if (IsMinableResource(item))
		{
			item.Mine = mine;
		}
	}

	[HarmonyPrefix]
	private static void Prefix(DB_Mgr __instance)
	{
		Vars.LogForHarmony.LogInfo("[Resource] Patching...");

		var db = __instance.m_Res_DB1;
		if (db == null)
		{
			Vars.LogForHarmony.LogError("[Resource] __instance.m_Res_DB1 == null");
			return;
		}

		if (!File.Exists(Vars.FilePathResource))
		{
			try
			{
				var sheet = db.sheets[0];

				var fileContentRaw = sheet.list
					.Where(item => item.Enable != 0)
					.ToList();
				var fileContent = fileContentRaw
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

				var jsonContentRaw = JsonConvert.SerializeObject(fileContentRaw, Formatting.Indented);
				var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

				File.WriteAllText(Vars.FilePathResourceRaw, jsonContentRaw);
				File.WriteAllText(Vars.FilePathResource, jsonContent);

				Vars.LogForHarmony.LogInfo("[Resource] Bump File Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Resource] Bump File Failed: {e}");
			}
		}
		else
		{
			try
			{
				var jsonContent = File.ReadAllText(Vars.FilePathResource);
				var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

				var sheet = db.sheets[0];

				foreach (var entry in fileContent)
				{
					var item = sheet.list.Find(item => item.Name == entry.Name);
					if (item == null)
					{
						Vars.LogForHarmony.LogWarning($"[Resource] Patch Failed: item {entry.Name} not found");
						continue;
					}

					if (item.Enable == 0)
					{
						Vars.LogForHarmony.LogInfo($"[Resource] Patch Skipped: item {entry.Name} not enabled");
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
						Vars.LogForHarmony.LogInfo(
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

					Vars.LogForHarmony.LogInfo(log);

					SetAbilityOfResource(ref item, entry.Ability);
					SetMineOfResource(ref item, entry.Mine);
					item.Material_A = entry.RecipeA.Material;
					item.Product_A = entry.RecipeA.Quantity;
					item.BP_A = entry.RecipeA.Workload;
					item.Material_B = entry.RecipeB.Material;
					item.Product_B = entry.RecipeB.Quantity;
					item.BP_B = entry.RecipeB.Workload;
					item.Material_C = entry.RecipeC.Material;
					item.Product_C = entry.RecipeC.Quantity;
					item.BP_C = entry.RecipeC.Workload;

					Vars.LogForHarmony.LogInfo("[Resource] Patch Succeed");
				}
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Resource] Patch Failed: {e}");
			}
		}
	}
}