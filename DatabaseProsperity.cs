using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;

namespace Ratopia;

[HarmonyPatch(typeof(DB_Mgr), "Prosperity_DB_Setting")]
public class DatabaseProsperity
{
	private class Entry
	{
		public string Name;
		public int NeedValue;
		public int Pop;
		public int CitizenAbilityValue;
		public int PolicyNum;
	}

	[HarmonyPrefix]
	private static void Prefix(DB_Mgr __instance)
	{
		Vars.LogForHarmony.LogInfo("[Prosperity] Patching...");

		var db = __instance.m_Prosperity_DB1;
		if (db == null)
		{
			Vars.LogForHarmony.LogError("[Prosperity] __instance.m_Item_DB1 == null");
			return;
		}

		if (!File.Exists(Vars.FilePathProsperity))
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

				File.WriteAllText(Vars.FilePathProsperity, jsonContent);
				Vars.LogForHarmony.LogInfo("[Prosperity] Bump File Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Prosperity] Bump File Failed: {e}");
			}
		}
		else
		{
			try
			{
				var jsonContent = File.ReadAllText(Vars.FilePathProsperity);
				var fileContent = JsonConvert.DeserializeObject<List<Entry>>(jsonContent);

				var sheet = db.sheets[0];

				foreach (var entry in fileContent)
				{
					var item = sheet.list.Find(item => item.Name == entry.Name);
					if (item == null)
					{
						Vars.LogForHarmony.LogWarning($"[Prosperity] Patch Failed: item {entry.Name} not found");
						continue;
					}

					if (
						item.NeedValue == entry.NeedValue &&
						item.Pop == entry.Pop &&
						item.CitizenAbilityValue == entry.CitizenAbilityValue &&
						item.PolicyNum == entry.PolicyNum
					)
					{
						Vars.LogForHarmony.LogInfo(
							$"[Prosperity] Patch Skipped: item [{entry.Name}] not changed"
						);
						continue;
					}

					Vars.LogForHarmony.LogInfo(
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

				Vars.LogForHarmony.LogInfo("[Prosperity] Patch Succeed");
			}
			catch (Exception e)
			{
				Vars.LogForHarmony.LogError($"[Prosperity] Patch Failed: {e}");
			}
		}
	}
}