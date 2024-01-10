using BBSeedsExtended.Patches;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI.AssetManager;
using System.IO;
using System;
using System.Text;
using UnityEngine;

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
		}

		public void SaveSeed()
		{
			string path = AssetManager.GetModPath(this); // Get folder
			Directory.CreateDirectory(path); // Create folder
			if (!string.IsNullOrEmpty(GameLoaderSingleton.seed))
				File.WriteAllText(Path.Combine(path, $"{Singleton<PlayerFileManager>.Instance.fileName}.txt"), Convert.ToBase64String(Encoding.UTF8.GetBytes(GameLoaderSingleton.seed)));
		}

		public void LoadSeed()
		{
			string path = Path.Combine(AssetManager.GetModPath(this), $"{Singleton<PlayerFileManager>.Instance.fileName}.txt");
			
			if (!File.Exists(path))
				return;

			string line = File.ReadAllText(path);
			
			try
			{
				GameLoaderSingleton.seed = Encoding.UTF8.GetString(Convert.FromBase64String(line));

				bool isInt = int.TryParse(GameLoaderSingleton.seed, out _);
				int skips = long.Parse(GameLoaderSingleton.seed).RoundLongVal(2, 2);
				GameLoaderSingleton.skips = skips == 0 && !isInt ? 1 : skips;
			}
			catch (Exception e)
			{
				Debug.LogWarning("Failed to grab data of the seed: " + e.Message);

				GameLoaderSingleton.seed = Singleton<CoreGameManager>.Instance.Seed().ToString();
				GameLoaderSingleton.skips = 0;
			}
		}

		public static BasePlugin i;
	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbseedextended";
		internal const string Name = "Baldi\'s Seed Extension";
		internal const string Version = "1.0.0";
	}
}
