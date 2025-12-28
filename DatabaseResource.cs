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

		// 名称
		// Helpers.StringToEnum<TileType>(Name)
		public string Name;

		// 类型
		// 如默认为0 ==> (ResCateogry)0 ==> ResCateogry.None
		// 如食物为1 ==> (ResCateogry)1 ==> ResCateogry.Food
		// 如生活用品为2 ==> (ResCateogry)2 ==> ResCateogry.Life
		// 如不可破坏为99 ==> (ResCateogry)99 ==> ResCateogry.Unbreakable
		// (ResCateogry)Category
		public int Category;

		// 资源提供的能力加成(以','分隔)
		// 只有食物和生活用品才有效
		// 如蛋糕(Cake)提供敏捷+3和+3智力和+10%食物消耗速度(DEX(3), INT(3), HUG(0.1)) (如果是-10%食物消耗速度则是HUG(-0.1))
		// Helpers.StringToEnum<Res_Ability>(ability)
		public string Ability;

		// 可以获取该材料的瓦片,例如Grass(3)表示挖取一个Grass可以得到三个产品(可以以,分隔指定多个瓦片)
		// Helpers.StringToEnum<TileType>(mine)
		public string Mine;

		// Material_A + Product_A + BP_A: 配方A的"所需材料" + "产品数量" + "工作量"
		// Material_B + Product_B + BP_B: 配方B的"所需材料" + "产品数量" + "工作量"
		// Material_C + Product_C + BP_C: 配方C的"所需材料" + "产品数量" + "工作量"
		// 例如
		// "Material_C": "Dirt(1)",
		// "Product_C": 20,
		// "BP_C": 50
		// 表示消耗一个Dirt和50工作量可以生成20个产品

		public Recipe RecipeA;
		public Recipe RecipeB;
		public Recipe RecipeC;
	}

	// ================================================
	// 判断资源类型
	// ================================================

	private static bool IsFoodResource(in Res_DB1.Param item)
	{
		return (ResCateogry)item.Category == ResCateogry.Food;
	}

	private static bool IsLifeResource(in Res_DB1.Param item)
	{
		return (ResCateogry)item.Category == ResCateogry.Life;
	}

	private static bool IsFoodOrLifeResource(in Res_DB1.Param item)
	{
		return IsFoodResource(item) || IsLifeResource(item);
	}

	private static bool IsMinableResource(in Res_DB1.Param item)
	{
		// todo: 更严格的检查
		return (ResCateogry)item.Category == ResCateogry.None;
	}

	// ================================================
	// 获取资源效果
	// ================================================

	private static string GetResourceAbility(in Res_DB1.Param item)
	{
		return IsFoodOrLifeResource(item)
			? item.Ability
			: "This resource is unusable, so no ability can be provided.(Any settings will be ignored)";
	}

	private static string GetResourceMine(in Res_DB1.Param item)
	{
		return IsMinableResource(item)
			? item.Mine
			: "This resource is unminable.(Any settings will be ignored)";
	}

	// ================================================
	// 设置资源效果
	// ================================================

	private static void SetResourceAbility(ref Res_DB1.Param item, string ability)
	{
		if (!IsFoodOrLifeResource(item))
		{
			return;
		}

		item.Ability = ability;
	}

	private static void SetResourceMine(ref Res_DB1.Param item, string mine)
	{
		if (!IsMinableResource(item))
		{
			return;
		}

		item.Mine = mine;
	}

	// ================================================
	// PATCH
	// ================================================

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
						Category = item.Category,
						Ability = GetResourceAbility(item),
						Mine = GetResourceMine(item),
						RecipeA = new Entry.Recipe
							{ Material = item.Material_A, Quantity = item.Product_A, Workload = item.BP_A },
						RecipeB = new Entry.Recipe
							{ Material = item.Material_B, Quantity = item.Product_B, Workload = item.BP_B },
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

					if (item.Category != entry.Category)
					{
						Vars.LogForHarmony.LogWarning(
							$"[Resource] Patch Failed: item {entry.Name} category mismatch ({item.Category} != {entry.Category})");
						continue;
					}

					if (
						(!IsFoodOrLifeResource(item) || GetResourceAbility(item) == entry.Ability) &&
						(!IsMinableResource(item) || GetResourceMine(item) == entry.Mine) &&
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
					if (IsFoodOrLifeResource(item))
					{
						log += $"\n\tAbility: [{GetResourceAbility(item)}] ==> [{entry.Ability}]";
					}

					if (IsMinableResource(item))
					{
						log += $"\n\tMine: [{GetResourceMine(item)}] ==> [{entry.Mine}]";
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

					SetResourceAbility(ref item, entry.Ability);
					SetResourceMine(ref item, entry.Mine);
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