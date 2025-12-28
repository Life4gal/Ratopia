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
		// 0 == 禁用
		// 1 == 默认可用
		// others == 满足条件时解锁
		public int Enable;

		// 名称
		public string Name;

		// 配方(以','分隔)
		// 如木弓(WoodBow)需要2木材+1绳子(Lumber(2), Rope(1))
		// Helpers.StringToEnum<TileType>(resource)
		public string Recipe;

		// 武器附加能力(以','分隔)
		// 如木弓(WoodBow)提供+2攻击力和+10%移动速度(ATK(2), SPD(0.1))
		// Helpers.StringToEnum<Res_Ability>(ability)
		// ATK(1) = 攻击力+1
		// DEF(1) = 防御力+1
		// HP(20) = HP+20
		// SPD(0.1) = 移动速度+10%
		// STR(1) = 力量+1
		// DEX(1) = 敏捷+1
		// INT(1) = 智力+1
		// PowerPropor(1) = 每点力量+1额外伤害
		// 	IntPropor(1) = 每点智力+1额外伤害
		// 	MoneyAtk(3000) = 国库每3000金钱+1额外伤害
		//
		// 	Com3_Per(0.5) = 连续攻击的第三次攻击+50%伤害
		// 	BloodDrain(2) = 攻击时恢复2点生命值
		// 	ElecAtk(5) = 5点电击伤害(攻击)
		// ElecThornB(4) = 5点点击反伤(被攻击)
		// ElecShield(1) = 1点护盾充电量
		// 	RushAtk(6) = 6点撞击伤害
		// 	HpGen(1) = 1点生命回复
		//
		// 	KnockB(1) = 击退敌人
		// 	RangeAtk(1) = 范围伤害(50%)
		// MakeSkull(1) = 击败敌人召唤骷髅鼠
		// 	Q_Rrr(1) = 复活
		// 	DrownX(1) = 水下呼吸
		// 	FallX(1) = 无坠落伤害
		// 	BurnX(1) = 无火焰伤害
		// 	Ratdrake(1) = 搬运受伤的鼠鼠直接回满血(鼠参)
		//
		// Constructing(200) = 建造和维修速度+200%
		// GatherMining(150) = 挖掘和采集的效率+150%
		// PlusMat(50) = 挖掘时有50%的几率获得额外资源
		// 	TP(3) = 可运输量+3
		// HaBuff(3) = 遇到的鼠鼠+3幸福度
		// 	SunPowerBuff(3) = 遇到的净化教徒鼠鼠+3力量
		// 	DarkIntBuff(3) = 遇到的暗影教徒鼠鼠+3智力
		public string Ability;
	}

	private class FileContentRaw
	{
		public List<Item_DB1.Param> Weapon;
		public List<Item_DB1.Param> Clothes;
		public List<Item_DB1.Param> Accessory;
	}

	private class FileContent
	{
		public List<Entry> Weapon;
		public List<Entry> Clothes;
		public List<Entry> Accessory;
	}

	private static List<Item_DB1.Param> FilterItem(in List<Item_DB1.Param> items)
	{
		return items
			.Where(item => item.Enable != 0)
			.ToList();
	}

	private static List<Entry> FilterItemToEntries(in List<Item_DB1.Param> items)
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

	private static void ApplyItemFromEntries(ref List<Item_DB1.Param> items, in List<Entry> entries)
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

				var weaponRaw = FilterItem(in weaponList);
				var clothRaw = FilterItem(in clothList);
				var accessoryRaw = FilterItem(in accessoryList);

				var weapon = FilterItemToEntries(in weaponList);
				var cloth = FilterItemToEntries(in clothList);
				var accessory = FilterItemToEntries(in accessoryList);

				var fileContentRaw = new FileContentRaw
					{ Weapon = weaponRaw, Clothes = clothRaw, Accessory = accessoryRaw };
				var fileContent = new FileContent
					{ Weapon = weapon, Clothes = cloth, Accessory = accessory };

				var jsonContentRaw = JsonConvert.SerializeObject(fileContentRaw, Formatting.Indented);
				var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

				File.WriteAllText(Vars.FilePathItemRaw, jsonContentRaw);
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

				ApplyItemFromEntries(ref weaponList, in weapon);
				ApplyItemFromEntries(ref clothList, in cloth);
				ApplyItemFromEntries(ref accessoryList, in accessory);

				Vars.LogForHarmony.LogInfo("[Item] Patch Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Item] Patch Failed: {e}");
			}
		}
	}
}