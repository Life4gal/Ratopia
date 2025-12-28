using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;

namespace Ratopia;

[HarmonyPatch(typeof(DB_Mgr), "QueenCharacter_DB_Setting")]
public class DatabaseQueenCharacter
{
	private class Entry
	{
		// 名称
		public string Name;

		// 女王角色附加能力(以','分隔)
		// 如战士(Warrior)提供+1攻击力和+20HP(ATK(1), HP(20))
		// Helpers.StringToEnum<Res_Ability>(ability)
		// EXP(0.2) = +20经验值获取
		// 	StartRP(3) = +3研究点数
		// 	ATK(1)/DEF(1) = +1攻击力/防御力
		// 	HP(20) = +20生命值
		// 	SPD(0.1) = +10%移动速度
		// 	TP(1) = +1运输量
		// 	CitizenG(1) = +1移民能力等级
		// 	CityHappy(1) = +1幸福度
		// 	StartPia(5000) = +5000初始存款
		// 	LoanPlus(3) = +3贷款产品数量
		// 	StartLP(1) = +1领导者点数
		// 	GetProsRefresh(1) = +1里程碑刷新次数
		// 	StartMap(10) = +10地图探索区域
		// 	StartRelation(10) = +10发现国家的友好度
		public string Ability;
	}

	[HarmonyPrefix]
	private static void Prefix(DB_Mgr __instance)
	{
		Vars.LogForHarmony.LogInfo("[QueenCharacter] Patching...");

		var db = __instance.m_QueenCharacter_DB;
		if (db == null)
		{
			Vars.LogForHarmony.LogError("[QueenCharacter] __instance.m_QueenCharacter_DB == null");
			return;
		}

		if (!File.Exists(Vars.FilePathQueenCharacter))
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
						Ability = item.Ability,
					})
					.ToList();

				var jsonContentRaw = JsonConvert.SerializeObject(fileContentRaw, Formatting.Indented);
				var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

				File.WriteAllText(Vars.FilePathQueenCharacterRaw, jsonContentRaw);
				File.WriteAllText(Vars.FilePathQueenCharacter, jsonContent);

				Vars.LogForHarmony.LogInfo("[QueenCharacter] Bump File Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[QueenCharacter] Bump File Failed: {e}");
			}
		}
		else
		{
			try
			{
				var jsonContent = File.ReadAllText(Vars.FilePathQueenCharacter);
				var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

				var sheet = db.sheets[0];

				foreach (var entry in fileContent)
				{
					var item = sheet.list.Find(item => item.Name == entry.Name);
					if (item == null)
					{
						Vars.LogForHarmony.LogWarning($"[QueenCharacter] Patch Failed: item {entry.Name} not found");
						continue;
					}

					if (item.Enable == 0)
					{
						Vars.LogForHarmony.LogInfo($"[QueenCharacter] Patch Skipped: item {entry.Name} not enabled");
						continue;
					}

					if (item.Ability == entry.Ability)
					{
						Vars.LogForHarmony.LogInfo(
							$"[QueenCharacter] Patch Skipped: item [{entry.Name}] not changed({entry.Ability})"
						);
						continue;
					}

					Vars.LogForHarmony.LogInfo(
						$"[QueenCharacter]\n" +
						$"{entry.Name}: " +
						$"\n\tAbility: [{item.Ability}] ==> [{entry.Ability}]"
					);

					item.Ability = entry.Ability;
				}

				Vars.LogForHarmony.LogInfo("[QueenCharacter] Patch Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[QueenCharacter] Patch Failed: {e}");
			}
		}
	}
}