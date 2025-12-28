using BepInEx;
using HarmonyLib;
using System.IO;

namespace Ratopia;

[BepInPlugin("Ratopia", "Ratopia", "1.0.0")]
public class RatopiaPlugin : BaseUnityPlugin
{
	private void Awake()
	{
		if (!Directory.Exists(Vars.PluginFolder))
		{
			Directory.CreateDirectory(Vars.PluginFolder);
		}

		var harmony = new Harmony("Ratopia");
		harmony.PatchAll(typeof(RatopiaPlugin).Assembly);
	}

	private void OnEnable()
	{
		Vars.LogForHarmony = Logger;
	}
}