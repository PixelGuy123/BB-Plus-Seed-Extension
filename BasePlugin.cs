using BBSeedsExtended.Patches;
using BepInEx;
using HarmonyLib;
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
			ogpath = Path.Combine(Application.streamingAssetsPath, "Modded", Info.Metadata.GUID);
			i = this;

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();
		}

		public void SaveSeed()
		{
			Directory.CreateDirectory(ogpath); // Create folder
			if (!string.IsNullOrEmpty(GameLoaderSingleton.seed))
				File.WriteAllText(Path.Combine(ogpath, $"{Singleton<PlayerFileManager>.Instance.fileName}.txt"), Convert.ToBase64String(Encoding.UTF8.GetBytes(GameLoaderSingleton.seed)));
		}

		public void LoadSeed()
		{
			string path = Path.Combine(ogpath, $"{Singleton<PlayerFileManager>.Instance.fileName}.txt");
			
			if (!File.Exists(path))
				return;

			string line = File.ReadAllText(path);
			
			try
			{
				GameLoaderSingleton.SetSeed(Encoding.UTF8.GetString(Convert.FromBase64String(line)));
			}
			catch (Exception e)
			{
				Debug.LogWarning("Failed to grab data of the seed: " + e.Message);

				GameLoaderSingleton.seed = Singleton<CoreGameManager>.Instance.Seed().ToString();
				GameLoaderSingleton.skips = 0;
			}
		}

		public static BasePlugin i;

		string ogpath = string.Empty;
	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbseedextended";
		internal const string Name = "Baldi\'s Seed Extension";
		internal const string Version = "1.0.0";
	}
}
