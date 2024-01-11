using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using TMPro;
using MTM101BaldAPI.Reflection;
using UnityEngine;
using BBSeedsExtended.Plugin;

namespace BBSeedsExtended.Patches
{

	[HarmonyPatch(typeof(GameLoader), "Initialize")]
	public class GameLoaderSingleton
	{
		private static void Prefix(GameLoader __instance, SeedInput ___seedInput)
		{
			instance = __instance;
			if (!Singleton<PlayerFileManager>.Instance.savedGameData.saveAvailable && ___seedInput.UseSeed) 
			{
				string val = (string)___seedInput.ReflectionGetVariable("currentValue");

				bool isInt = int.TryParse(val, out _);
				int skips = long.Parse(val).RoundLongVal(2, 2);
				GameLoaderSingleton.skips = skips == 0 && !isInt ? 1 : skips; // Just to guarantee 1 if it was still a long value
				seed = val;
			}
		}

		public static GameLoader instance;

		public static int skips;

		public static string seed;
	}

	[HarmonyPatch(typeof(EndlessMapOverview), "LoadLevel")]
	public class GameLoaderAddition
	{
		private static void Prefix(EndlessMapOverview __instance)
		{
			if (__instance.seedInput.UseSeed)
			{
				string val = (string)__instance.seedInput.ReflectionGetVariable("currentValue");
				bool isInt = int.TryParse(val, out _);
				int skips = long.Parse(val).RoundLongVal(2, 2);
				GameLoaderSingleton.skips = skips == 0 && !isInt ? 1 : skips; // 0 if it's not a long value (to maintain the og seeds)
				GameLoaderSingleton.seed = val;
			}
		}
	}

	[HarmonyPatch(typeof(SeedInput))]
	public class MainPatch_SeedInput
	{
		/*[HarmonyPrefix]
		[HarmonyPatch("Awake")]
		private static void RegisterMyself(SeedInput __instance) // Get it inside the game
		{
			//if (!seed.ContainsKey(__instance.name))
			//{
			//	seed.Add(__instance.name, "0");
			//	amountOfNexts.Add(__instance.name, 0);
			//}
		}
		*/

		[HarmonyPrefix]
		[HarmonyPatch("UpdateValue")]
		private static bool UpdateValuePatch(ref string ___currentValue, ref int ___value) // Basically just allows long values
		{
			if (___currentValue == "-" || ___currentValue == "")
			{
				___value = 0;
				return false;
			}
			if (int.TryParse(___currentValue, out int res))
			{
				___value = res;
				___currentValue = ___value.ToString();
				//amountOfNexts[__instance.name] = 0;
			}
			else if (long.TryParse(___currentValue, out long lres))
			{
				___currentValue = lres.ToString();
				___value = lres.RoundLongVal(2, 2); // Just mod the value, DOES NOT CHANGE THE CURRENT VALUE AT ALL
				//amountOfNexts[__instance.name] = Math.Abs(___value);
			}

			//seed[__instance.name] = ___currentValue;

			return false;
		}

		[HarmonyTranspiler]
		[HarmonyPatch("ValueIsValid")] // Note: use ldc_I8 for long values
		private static IEnumerable<CodeInstruction> ChangeSomeParameters(IEnumerable<CodeInstruction> instructions) =>
			new CodeMatcher(instructions).SearchForward(x => x.opcode == OpCodes.Ldc_I4_S).Set(OpCodes.Ldc_I4_S, long.MinValue.ToString().Length) // Change the length to the minvalue length as string
			.SearchForward(x => x.opcode == OpCodes.Ldc_I4).Set(OpCodes.Ldc_I8, long.MaxValue) // Basically search the first ldc, then change to long.maxvalue
				.SearchForward(x => x.opcode == OpCodes.Ldc_I4).Set(OpCodes.Ldc_I8, long.MinValue).InstructionEnumeration(); // Search the second one and replace with minvalue instead

		[HarmonyTranspiler]
		[HarmonyPatch("SetValue", [typeof(string)])] // Forgot about ambiguous method
		private static IEnumerable<CodeInstruction> SetValueFor64Bits(IEnumerable<CodeInstruction> instructions) =>
			new CodeMatcher(instructions).SearchForward(x => x.opcode == OpCodes.Ldc_I4).Set(OpCodes.Ldc_I8, long.MaxValue) // Basically search the first ldc, then change to long.maxvalue
				.SearchForward(x => x.opcode == OpCodes.Ldc_I4).Set(OpCodes.Ldc_I8, long.MinValue).InstructionEnumeration(); // Search the second one and replace with minvalue instead

		[HarmonyFinalizer]
		[HarmonyPatch("SetValue", [ typeof(string) ])]
		private static Exception WhyYouUsingConvertAnyways() =>
			null; // Shut the exception up



		//readonly static Dictionary<string, string> seed = [];
		//readonly static Dictionary<string, int> amountOfNexts = [];

		//public static Dictionary<string, string> Seed => seed;
		//public static Dictionary<string, int> Nexts => amountOfNexts;
		
	}

	[HarmonyPatch(typeof(CoreGameManager))]
	internal class Use64Bit // Basically make random 64 bit
	{
		[HarmonyPatch("SetRandomSeed")]
		[HarmonyPrefix]
		private static bool UseRandom64bit(ref int ___seed)
		{
			if (GameLoaderSingleton.instance != null)
			{
				long val = new System.Random().NextInt64();
				val = UnityEngine.Random.value > 0.5f ? val : -val;
				___seed = val.RoundLongVal(2, 2);
				GameLoaderSingleton.seed = val.ToString();
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(ElevatorScreen), "Start")]
	internal class Display64bitSeed_Elevator
	{
		private static void Prefix(ref TMP_Text ___seedText)
		{
			___seedText.autoSizeTextContainer = true;
			___seedText.alignment = TextAlignmentOptions.Center;
			___seedText.transform.localPosition = new Vector3(67.45f, 94.25f, 0);
		}
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var target = AccessTools.Method(typeof(Singleton<CoreGameManager>), "get_Instance");
			return new CodeMatcher(instructions).SearchForward(x => x.Is(OpCodes.Call, target))
				.RemoveInstructions(5)
				.Insert(Transpilers.EmitDelegate(() => GameLoaderSingleton.seed)) // Basically just removes all the code that gets the coregamemanager and replace with this
				.InstructionEnumeration();
		}
	}

	[HarmonyPatch(typeof(PauseReset), "OnEnable")]
	internal class Display64BitSeed_Pause
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var target = AccessTools.Method(typeof(Singleton<CoreGameManager>), "get_Instance");
			return new CodeMatcher(instructions).SearchForward(x => x.Is(OpCodes.Call, target))
				.RemoveInstructions(5)
				.Insert(Transpilers.EmitDelegate(() => GameLoaderSingleton.seed)) // Basically just removes all the code that gets the coregamemanager and replace with this
				.InstructionEnumeration();
		}
	}

	[HarmonyPatch(typeof(LevelGenerator))]

	internal class GeneratorPatch
	{
		[HarmonyPatch("StartGenerate")]
		private static void Prefix(LevelGenerator __instance)
		{
			gen = __instance;
		}

		static LevelGenerator gen;

		[HarmonyPatch("Generate", MethodType.Enumerator)]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			static void SkipRNGTimes()
			{
				int max = Math.Abs(GameLoaderSingleton.skips) / divider;
				for (int i = 0; i < max; i++)
					gen.controlledRNG.Next(); // Skips here
			}

			var target = AccessTools.Field(typeof(LevelBuilder), "controlledRNG");

			return new CodeMatcher(instructions).SearchForward(x => x.Is(OpCodes.Stfld, target)) // Find first ControlledRNG thingy
				.Advance(1) // Advance 1 step
				.Insert(Transpilers.EmitDelegate(SkipRNGTimes)) // Put the delegate
				.SearchForward(x => x.Is(OpCodes.Stfld, target)) // Repeat this again
				.Advance(1)
				.Insert(Transpilers.EmitDelegate(SkipRNGTimes))
				.InstructionEnumeration(); // return back
		}

		const int divider = 10;
	}

	[HarmonyPatch(typeof(PlayerFileManager), "Save", [typeof(float)])]
	internal class PlayerManagerPatch
	{
		private static void Prefix() =>
			BasePlugin.i.SaveSeed();
		
	}

	[HarmonyPatch(typeof(MainModeButtonController), "OnEnable")]
	internal class MainMenuButtonPatch
	{
		private static void Prefix()
		{
			if (Singleton<PlayerFileManager>.Instance.savedGameData.saveAvailable)
				BasePlugin.i.LoadSeed();
		}
	}



}
