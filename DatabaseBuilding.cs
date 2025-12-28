using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ratopia;

[HarmonyPatch(typeof(DB_Mgr), "Build_DB_Setting")]
public class DatabaseBuilding
{
	private class Entry
	{
		// 名称
		// Helpers.StringToEnum<BuildingName>(Name)
		public string Name;

		// 建筑类型
		// 如生产建筑为3 ==> (BuildCategory)3 ==> BuildCategory.Product
		// (BuildCategory)(Category)
		public int Category;

		// 宽度(不影响贴图大小)
		public int Width;

		// Height: 建筑高度(不影响贴图大小)
		public int Height;

		// 建筑生命值
		public int HP;

		// 建造花费
		public int Cost;

		// 电力花费
		public int ElecCost;

		// 工资(一般是兵营这种驻守建筑)
		public int Payment;

		// 建造材料(以','分隔)
		// 如仓库(Storage)需要2石材+4木材(StoneBrick(2), Lumber(4))
		// Helpers.StringToEnum<TileType>(material)
		public string Material;

		// 建筑提供的职业被动能力(以','分隔)
		// 如伐木营地(LumberCamp)提供+1运输量(TP(1))
		// Helpers.StringToEnum<Res_Ability>(passive)
		public string JobPassive;

		// 建筑提供的能力加成(以','分隔)
		// 如学习(School)提供智力+3(INT(3))
		// Helpers.StringToEnum<Res_Ability>(ability)
		public string Effect_Ability;

		// AbilityCode_A + Effect_Value3
		// Helpers.StringToEnum<BuildAbility>(AbilityCode_A)
		// 原材料生产建筑和产品生产建筑(BuildCategory.RawMat / BuildCategory.Product)
		//     其AbilityCode_A为Masonry
		//     其Effect_Value3为产品(以','分隔)(Helpers.StringToEnum<TileType>(product))
		//     例如石匠厂(Masonry)的Effect_Value3为石材+沙子(StoneBrick(A), Sand(A))
		// 服务建筑(BuildCategory.Service)
		//     其AbilityCode_A为Atelier
		//     其Effect_Value3为需求(以','分隔)(Helpers.StringToEnum<TileType>(requires))
		//     例如学校(School)的Effect_Value3为一个工具(Tool(1))
		// 原材料收集建筑(BuildCategory.RawMat)
		//     其AbilityCode_A为LumberCamp
		//     其所有建筑单独处理
		//     伐木营地(LumberCamp) ==> 仅限植物.树木(产品的Category == PlantCategory.Tree,且无视指定的数量,直接设置为0)
		//         如: TreePlant(), JungleTreePlant(), ThornTreePlant(), JewelTreePlant(), CoralTreePlant(), AshTreePlant()
		//     树液营地(SapCamp) == > 仅限植物.树液(产品于DB_Mgr.List_PlantDB列表中,且无视指定的数量,直接设置为0)
		//         如: TreePlant(), JewelTreePlant(), CoralTreePlant(), AshTreePlant()
		//     收集营地/种植营地(GatheringCamp/PlantationCamp) ==> 无视指定的数量,直接设置为0, 一般是植物(Helpers.StringToEnum<TileType>(product))
		//         如: GrainPlant(), GrassPlant(), FlowerPlant()
		//     储水罐(WaterTank) ==> 与一般的生产建筑类似,储水罐生产水(Water()),但是理论上支持其他组合,例如和石匠厂一样(StoneBrick(A), Sand(A)),省略ABC则默认为A
		// 种植建筑/墓地(BuildCategory.RawMat)
		//     其AbilityCode_A为Minigarden/Gravegarden
		//     Minigarden ==> 小/大花盆(Minigarden/Treegarden)
		//     Gravegarden ==> 墓地(Gravegarden)
		//     处理方式与收集营地/种植营地(GatheringCamp/PlantationCamp)相同
		// 猎杀动物/钓鱼/动物传送门(BuildCategory.RawMat)
		//     其AbilityCode_A为HunterHut/AnimalDimensionGate
		//     HunterHut ==> 猎人营地/钓鱼台(HunterHut/Fishery)
		//     AnimalDimensionGate ==> 动物传送门(AnimalDimensionGate)
		//     处理方式与收集营地/种植营地(GatheringCamp/PlantationCamp)相同
		// 轨道(BuildCategory.Base)
		//     铁轨/电梯井
		//     什么都不提供

		public string AbilityCode;
		public string EffectValue;

		// AbilityCode_B
		// Helpers.StringToEnum<DesireName>(AbilityCode_B)
		// 满足指定公民需求的建筑(BuildCategory.Base / BuildCategory.Service)
		// Fun/Hunger/Clean/Life/Anything ==> 乐趣/食物/卫生/生活用品/?(医院/寺庙)
		// Fatigue/Happy/Buff ==> 疲劳(床)/无/无

		// AbilityCode_C
		// Helpers.StringToEnum<DungeonGroup>(AbilityCode_C)

		// Effect_Value1 / Effect_Value2 / Effect_Value3
		// PN, N为有符号整数,例如P10,P-10
		// 
		// 当且仅当Helpers.StringToEnum<BuildAbility>(AbilityCode_A)为以下值时:
		// BuildAbility.Road
		// BuildAbility.ConstructionOffice
		// BuildAbility.MiningCamp
		// BuildAbility.Post
		// BuildAbility.Deco_Bench
		// BuildAbility.Deco_Fountain
		// BuildAbility.Ladder
		// BuildAbility.Roof
		// BuildAbility.Store
		// BuildAbility.Rail
		// BuildAbility.Deco_Paint
		// BuildAbility.Deco_QueenChair
		// Helpers.IsDungeonAbility(ability)
		// BuildAbility.Deco_Ceiling
		// BuildAbility.Dynamo
		// BuildAbility.LiftRail
		// BuildAbility.Deco_Statue
		// BuildAbility.LumberCamp
		// BuildAbility.Minigarden
		// BuildAbility.DungeonTotem
		// BuildAbility.Battery
		// BuildAbility.PigeonMailbox
		// BuildAbility.Wallpaper
		// BuildAbility.Lift
		// BuildAbility.Substation
		// BuildAbility.Deco_PaintAch
		//
		// Effect_Value1 生效
		// 仓库(Storage): 提供40格容量
		//     "AbilityCode_A": "Store" ==> BuildAbility.Store
		//     "AbilityCode_B": "-"
		//     "AbilityCode_C": "-"
		//     "Effect_Value1": "P40" ==> 40格容量
		//     "Effect_Value2": "-"
		//     "Effect_Value3": "-"
		//     "Effect_Ability": "-" 
		//
		// 当且仅当Helpers.StringToEnum<BuildAbility>(AbilityCode_A)为以下值时:
		// BuildAbility.Gravegarden
		// BuildAbility.CarrierStation
		// BuildAbility.House
		// BuildAbility.Barricade
		// BuildAbility.RatronStation
		// BuildAbility.Masonry
		// BuildAbility.Mine
		// BuildAbility.QueenBed
		// BuildAbility.Tunnel
		// BuildAbility.WirelessCharger
		// BuildAbility.Canopy
		// BuildAbility.AnimalDimensionGate
		// 
		// Effect_Value1 + Effect_Value2  生效
		// 床(House): 生命值恢复+1,行动力恢复速度+100%
		//     "AbilityCode_A": "House" ==> BuildAbility.House
		//     "AbilityCode_B": "Fatigue"
		//     "AbilityCode_C": "-"
		//     "Effect_Value1": "P100" ==> 行动力恢复速度+100%
		//     "Effect_Value2": "P1" ==> 生命值恢复+1
		//     "Effect_Value3": "-"
		//     "Effect_Ability": "-" 
		// 
		// 当且仅当Helpers.StringToEnum<BuildAbility>(AbilityCode_A)为以下值时:
		// BuildAbility.GraveStone
		// BuildAbility.SkillTotem
		// BuildAbility.Trap
		// BuildAbility.QueenOnly
		// BuildAbility.BandStand
		// BuildAbility.Atelier(!)
		// BuildAbility.Ziggurat(!)
		// BuildAbility.SpiritRestingplace(!)
		// BuildAbility.FirecrackerSet(!)
		// 
		// Effect_Value1 + Effect_Value2  + Effect_Value3 生效
		// 其中带(!)的支持Effect_Value3为列表(Helpers.StringToEnum<TileType>(value)),而不是PN
		// 学校(School): 价格25, 需要工具x1, 提供10乐趣,智力+3
		//     "AbilityCode_A": "Atelier" ==> BuildAbility.Atelier
		//     "AbilityCode_B": "Fun" ==> 提供乐趣
		//     "AbilityCode_C": "-"
		//     "Effect_Value1": "P25" ==> 价格25
		//     "Effect_Value2": "P10" ==> 提供10乐趣
		//     "Effect_Value3": "Tool(1)" ==> 需要工具x1
		//     "Effect_Ability": "INT(3)" ==> 智力+3
		// 酒馆(Pub): 价格35,需要啤酒x1,提供30乐趣,食物+20,乐趣消耗速度-10%
		//     "AbilityCode_A": "Atelier" ==> BuildAbility.Atelier
		//     "AbilityCode_B": "Fun" ==> 提供乐趣
		//     "AbilityCode_C": "-"
		//     "Effect_Value1": "P35" ==> 价格25
		//     "Effect_Value2": "P30" ==> 提供30乐趣
		//     "Effect_Value3": "Beer(1)" ==> 需要啤酒x1
		//     "Effect_Ability": "HUG_TICK(20), FUN(-0.1)" ==> 20食物,乐趣消耗速度-10%
		// 
		// 当且仅当Helpers.StringToEnum<BuildAbility>(AbilityCode_A)*不*为以下值且不满足上面的条件时:
		// BuildAbility.Toilet
		// BuildAbility.TaxOffice
		// BuildAbility.Barrack
		// BuildAbility.CitizenCave
		// BuildAbility.AssembleStation
		// BuildAbility.BuffBuilding
		// BuildAbility.BuffPlant
		// BuildAbility.InvasionFlag
		// BuildAbility.MercenaryCamp
		// BuildAbility.AreaTotem
		// BuildAbility.AreaSleepStone
		// BuildAbility.AreaEatStone
		// BuildAbility.AreaFunStone
		// BuildAbility.AreaCleanStone
		// BuildAbility.AreaLifeStone
		// BuildAbility.SunTotem
		// BuildAbility.WeddingStatue
		// BuildAbility.Almshouse
		// BuildAbility.Chapel
		//
		// Effect_Value1 + Effect_Value2  生效
		//
		// 具体作用待研究
	}

	// ================================================
	// 判断建筑类型
	// ================================================

	// 原材料生产建筑和产品生产建筑
	private static bool IsBuildingCanProduction(in Building_DB1.Param item)
	{
		var category = (BuildCategory)item.Category;

		if (category != BuildCategory.RawMat && category != BuildCategory.Product)
		{
			return false;
		}

		return item.AbilityCode_A == nameof(BuildAbility.Masonry);
	}

	// 服务建筑
	private static bool IsBuildingCanService(in Building_DB1.Param item)
	{
		var category = (BuildCategory)item.Category;

		if (category != BuildCategory.Service)
		{
			return false;
		}

		return item.AbilityCode_A == nameof(BuildAbility.Atelier);
	}

	private static bool IsBuildingHasEffect(in Building_DB1.Param item)
	{
		return IsBuildingCanProduction(item) || IsBuildingCanService(item);
	}

	// ================================================
	// 获取建筑效果
	// ================================================

	private static string GetBuildingEffect(in Building_DB1.Param item)
	{
		return
			IsBuildingHasEffect(item)
				? item.Effect_Value3
				: "This building neither produces products nor provides services; therefore, it is unnecessary to establish it.(Any settings will be ignored)";
	}

	// ================================================
	// 设置建筑效果
	// ================================================

	private static void SetBuildingEffect(ref Building_DB1.Param item, string effect)
	{
		if (!IsBuildingHasEffect(item))
		{
			return;
		}

		item.Effect_Value3 = effect;
	}

	// ================================================
	// PATCH
	// ================================================

	[HarmonyPrefix]
	private static void Prefix(DB_Mgr __instance)
	{
		Vars.LogForHarmony.LogInfo("[Building] Patching...");

		var db = __instance.m_Building_DB1;
		if (db == null)
		{
			Vars.LogForHarmony.LogError("[Building] __instance.m_Building_DB1 == null");
			return;
		}

		if (!File.Exists(Vars.FilePathBuilding))
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
						Width = item.Width,
						Height = item.Height,
						HP = item.HP,
						Cost = item.Cost,
						ElecCost = item.ElecCost,
						Payment = item.Payment,
						Material = item.Material,
						JobPassive = item.JobPassive,
						Effect_Ability = item.Effect_Ability,
						AbilityCode = item.AbilityCode_A,
						EffectValue = GetBuildingEffect(in item),
					})
					.ToList();

				var jsonContentRaw = JsonConvert.SerializeObject(fileContentRaw, Formatting.Indented);
				var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

				File.WriteAllText(Vars.FilePathBuildingRaw, jsonContentRaw);
				File.WriteAllText(Vars.FilePathBuilding, jsonContent);

				Vars.LogForHarmony.LogInfo("[Building] Bump File Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Building] Bump File Failed: {e}");
			}
		}
		else
		{
			try
			{
				var jsonContent = File.ReadAllText(Vars.FilePathBuilding);
				var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

				var sheet = db.sheets[0];

				foreach (var entry in fileContent)
				{
					var item = sheet.list.Find(item => item.Name == entry.Name);
					if (item == null)
					{
						Vars.LogForHarmony.LogWarning($"[Building] Patch Failed: item {entry.Name} not found");
						continue;
					}

					if (item.Enable == 0)
					{
						Vars.LogForHarmony.LogInfo($"[Building] Patch Skipped: item {entry.Name} not enabled");
						continue;
					}

					if (item.Category != entry.Category)
					{
						Vars.LogForHarmony.LogWarning(
							$"[Building] Patch Failed: item {entry.Name} category mismatch ({item.Category} != {entry.Category})");
						continue;
					}

					if (item.AbilityCode_A != entry.AbilityCode)
					{
						Vars.LogForHarmony.LogWarning(
							$"[Building] Patch Failed: item {entry.Name} ability mismatch ({item.AbilityCode_A} != {entry.AbilityCode})");
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
						item.JobPassive == entry.JobPassive &&
						item.Effect_Ability == entry.Effect_Ability &&
						(!IsBuildingHasEffect(in item) || GetBuildingEffect(in item) == entry.EffectValue)
					)
					{
						Vars.LogForHarmony.LogInfo(
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
						$"\n\tMaterial: [{item.Material}] ==> [{entry.Material}]" +
						$"\n\tJobPassive: [{item.JobPassive}] ==> [{entry.JobPassive}]" +
						$"\n\tEffect_Ability: [{item.Effect_Ability}] ==> [{entry.Effect_Ability}]";
					if (IsBuildingCanProduction(in item))
					{
						log += $"\n\tProducts: [{GetBuildingEffect(in item)}] ==> [{entry.EffectValue}]";
					}
					else if (IsBuildingCanService(in item))
					{
						log += $"\n\tService Consumption: [{GetBuildingEffect(in item)}] ==> [{entry.EffectValue}]";
					}

					Vars.LogForHarmony.LogInfo(log);

					item.Width = entry.Width;
					item.Height = entry.Height;
					item.HP = entry.HP;
					item.Cost = entry.Cost;
					item.ElecCost = entry.ElecCost;
					item.Payment = entry.Payment;
					item.Material = entry.Material;
					item.JobPassive = entry.JobPassive;
					item.Effect_Ability = entry.Effect_Ability;
					SetBuildingEffect(ref item, entry.EffectValue);
				}

				Vars.LogForHarmony.LogInfo("[Building] Patch Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Building] Patch Failed: {e}");
			}
		}
	}
}