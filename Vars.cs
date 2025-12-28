using BepInEx;
using BepInEx.Logging;
using System.IO;

namespace Ratopia;

public static class Vars
{
	public static ManualLogSource LogForHarmony;

	public static readonly string PluginFolder = Path.Combine(Paths.PluginPath, "Ratopia");

	public static readonly string FilePathQueenCharacter = Path.Combine(PluginFolder, "QueenCharacter.json");
	public static readonly string FilePathProsperity = Path.Combine(PluginFolder, "Prosperity.json");

	public static readonly string FilePathResource = Path.Combine(PluginFolder, "Resource.json");

	// public static readonly string FilePathPlant = Path.Combine(PluginFolder, "Plant.json");
	public static readonly string FilePathBuilding = Path.Combine(PluginFolder, "Building.json");

	public static readonly string FilePathItem = Path.Combine(PluginFolder, "Item.json");
	// public static readonly string FilePathRatron = Path.Combine(PluginFolder, "Ratron.json");
	// public static readonly string FilePathRecipe = Path.Combine(PluginFolder, "Recipe.json");
}