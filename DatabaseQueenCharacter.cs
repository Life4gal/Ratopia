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
		public string Name;
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

				var fileContent = sheet.list
					.Where(item => item.Enable != 0)
					.Select(item => new Entry
					{
						Name = item.Name,
						Ability = item.Ability,
					})
					.ToList();
				var jsonContent = JsonConvert.SerializeObject(fileContent, Formatting.Indented);

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