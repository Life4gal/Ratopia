using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ratopia;

[HarmonyPatch(typeof(DB_Mgr), "Plant_DB_Setting")]
public class DatabasePlant
{
	private class Entry
	{
		// 名称
		// Helpers.StringToEnum<TileType>(Name)
		public string Name;

		// 宽度(不影响贴图大小)
		public int Width;

		// Height: 建筑高度(不影响贴图大小)
		public int Height;

		// 生长时间
		public int GrowTime;

		// 植物类型(Tree/Grass/Install)
		// 目前只有海葵(SeaanemonePlant)是Install类型
		// Helpers.StringToEnum<PlantCategory>(category)
		public string Category;

		// 能力(以','分隔)
		// 例如海葵(SeaanemonePlant)生产1水(BK_MakeWater(1))
		// Helpers.StringToEnum<Res_Ability>(ability)
		public string Ability;

		// 产品(以','分隔)
		// 不确定如何确定选取那个产品
		// Helpers.StringToEnum<TileType>(product)
		// WorldObject.Make_TObj ==> this.m_Level == ? ==> this.m_Info.List_ProductN
		public string Product_1;
		public string Product_2;
		public string Product_3;

		// 掠食者
		// 例如兔子会吃麦子
		// Helpers.StringToEnum<TileType>(predator)
		public string Predator;

		// 树液采集(以','分隔)
		// 树液/没药/橡胶/硫磺
		// 如丛林树(JungleTreePlant)生产1橡胶(Rubber(1))
		public string Sap;

		// 采集物(以','分隔)
		// 例如清除草丛(非100%成熟)可以得到1个草籽
		public string Recipe;

		// 栽培物(以','分隔)
		// 例如1个草籽可以栽培一个草丛(在小菜园中)
		public string Seed;
	}

	[HarmonyPrefix]
	private static void Prefix(DB_Mgr __instance)
	{
		Vars.LogForHarmony.LogInfo("[Plant] Patching...");

		var db = __instance.m_Plant_DB1;
		if (db == null)
		{
			Vars.LogForHarmony.LogError("[Plant] __instance.m_Plant_DB1 == null");
			return;
		}

		if (!File.Exists(Vars.FilePathPlant))
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
						Width = item.Width,
						Height = item.Height,
						GrowTime = item.GrowTime,
						Category = item.Category,
						Ability = item.Ability,
						Product_1 = item.Product_1,
						Product_2 = item.Product_2,
						Product_3 = item.Product_3,
						Predator = item.Predator,
						Sap = item.Sap,
						Recipe = item.Recipe,
						Seed = item.Seed
					})
					.ToList();

				var jsonContentRaw = JsonConvert.SerializeObject(fileContentRaw, Formatting.Indented);
				var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

				File.WriteAllText(Vars.FilePathPlantRaw, jsonContentRaw);
				File.WriteAllText(Vars.FilePathPlant, jsonContent);

				Vars.LogForHarmony.LogInfo("[Plant] Bump File Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Plant] Bump File Failed: {e}");
			}
		}
		else
		{
			try
			{
				var jsonContent = File.ReadAllText(Vars.FilePathPlant);
				var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

				var sheet = db.sheets[0];

				foreach (var entry in fileContent)
				{
					var item = sheet.list.Find(item => item.Name == entry.Name);
					if (item == null)
					{
						Vars.LogForHarmony.LogWarning($"[Plant] Patch Failed: item {entry.Name} not found");
						continue;
					}

					if (item.Enable == 0)
					{
						Vars.LogForHarmony.LogInfo($"[Plant] Patch Skipped: item {entry.Name} not enabled");
						continue;
					}

					if (item.Category != entry.Category)
					{
						Vars.LogForHarmony.LogWarning(
							$"[Plant] Patch Failed: item {entry.Name} category mismatch ({item.Category} != {entry.Category})");
						continue;
					}

					if (
						item.Width == entry.Width &&
						item.Height == entry.Height &&
						item.GrowTime == entry.GrowTime &&
						item.Ability == entry.Ability &&
						item.Product_1 == entry.Product_1 &&
						item.Product_2 == entry.Product_2 &&
						item.Product_3 == entry.Product_3 &&
						item.Predator == entry.Predator &&
						item.Sap == entry.Sap &&
						item.Recipe == entry.Recipe &&
						item.Seed == entry.Seed
					)
					{
						Vars.LogForHarmony.LogInfo(
							$"[Plant] Patch Skipped: item [{entry.Name}] not changed"
						);
						continue;
					}

					Vars.LogForHarmony.LogInfo(
						$"[Plant]\n" +
						$"{entry.Name}: " +
						$"\n\tWidth: [{item.Width}] ==> [{entry.Width}]" +
						$"\n\tHeight: [{item.Height}] ==> [{entry.Height}]" +
						$"\n\tGrowTime: [{item.GrowTime}] ==> [{entry.GrowTime}]" +
						$"\n\tAbility: [{item.Ability}] ==> [{entry.Ability}]" +
						$"\n\tProduct_1: [{item.Product_1}] ==> [{entry.Product_1}]" +
						$"\n\tProduct_2: [{item.Product_2}] ==> [{entry.Product_2}]" +
						$"\n\tProduct_3: [{item.Product_3}] ==> [{entry.Product_3}]" +
						$"\n\tPredator: [{item.Predator}] ==> [{entry.Predator}]" +
						$"\n\tSap: [{item.Sap}] ==> [{entry.Sap}]" +
						$"\n\tRecipe: [{item.Recipe}] ==> [{entry.Recipe}]" +
						$"\n\tSeed: [{item.Seed}] ==> [{entry.Seed}]"
					);

					item.Width = entry.Width;
					item.Height = entry.Height;
					item.GrowTime = entry.GrowTime;
					item.Ability = entry.Ability;
					item.Product_1 = entry.Product_1;
					item.Product_2 = entry.Product_2;
					item.Product_3 = entry.Product_3;
					item.Predator = entry.Predator;
					item.Sap = entry.Sap;
					item.Recipe = entry.Recipe;
					item.Seed = entry.Seed;
				}

				Vars.LogForHarmony.LogInfo("[Plant] Patch Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Plant] Patch Failed: {e}");
			}
		}
	}
}