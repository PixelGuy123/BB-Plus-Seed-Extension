using BBSeedsExtended.Patches;
using BepInEx;
using HarmonyLib;
using System.IO;
using System;
using UnityEngine;
using MTM101BaldAPI.SaveSystem;

namespace BBSeedsExtended.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
	public class BasePlugin : BaseUnityPlugin
	{
		void Awake()
		{
			i = this;

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();

			ModdedSaveGame.AddSaveHandler(new SavedSeedInBinary(Info));
		}

		public static BasePlugin i;

	}

	internal class SavedSeedInBinary(PluginInfo info) : ModdedSaveGameIOBinary
	{
		readonly PluginInfo _pg = info;
		public override PluginInfo pluginInfo => _pg;

		public override void Reset()
		{
		}

		public override void Load(BinaryReader reader)
		{
			try
			{
				GameLoaderSingleton.SetSeed(reader.ReadString());
			}
			catch (Exception e)
			{
				Debug.LogWarning("Failed to grab data of the seed: " + e.Message);

				GameLoaderSingleton.seed = Singleton<CoreGameManager>.Instance.Seed().ToString();
				GameLoaderSingleton.skips = 0;
			}
		}

		public override void Save(BinaryWriter writer)
		{
			writer.Write(GameLoaderSingleton.seed);
		}
	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbseedextended";
		internal const string Name = "Baldi\'s Seed Extension";
		internal const string Version = "1.0.2.1";
	}
}
