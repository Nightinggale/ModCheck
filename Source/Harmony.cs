using Harmony;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using System.Reflection.Emit;

namespace ModCheck
{
    public sealed class HarmonyStarter : Mod
    {
        public HarmonyStarter(ModContentPack content) : base (content)
        {
            // only use Harmony on the newest version of the DLL
            if (VersionChecker.IsNewestVersion())
            {
                var harmony = HarmonyInstance.Create("com.rimworld.modcheck");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                // setup table of patch ownership
                Memory.Instance.init();
            }
        }
    }
    
    [HarmonyPatch(typeof(RimWorld.MainMenuDrawer))]
    [HarmonyPatch("MainMenuOnGUI")]
    class DoneLoading
    {
        static void Postfix()
        {
            Memory.Clear();
        }
    }

    [HarmonyPatch(typeof(Verse.LoadedModManager))]
    [HarmonyPatch("ApplyPatches")]
    class VanillaPatching
    {
        static bool Prefix()
        {
            // Blank the current mod/file since it's not present for vanilla patching.
            Memory.Instance.resetModAndFile();
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> iList = new List<CodeInstruction>(instructions);

            int iLength = iList.Count;
            if (iLength != 33)
            {
                Log.Error("[ModCheck] Internal failure patching Verse.LoadedModManager.ApplyPatches");
                for (int i = 0; i < iLength; ++i)
                {
                    yield return iList[i];
                }
            }
            else
            {
                // keep the method intact, but add calls to Memory to tell when patching is started or stopped.
                // vanilla has been nice and added just two nop operations. Even better, they are at the right place for making the calls.
                // The nop operations are replaced with the calls here.
                bool First = true;
                for (int i = 0; i < iList.Count; ++i)
                {
                    if (iList[i].opcode == OpCodes.Nop)
                    {
                        if (First)
                        {
                            First = false;
                            CodeInstruction instruction = new CodeInstruction(OpCodes.Call);
                            if (Prefs.LogVerbose)
                            {
                                // start the timer as well as incrementing the counter
                                instruction.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.startPatchingWithTimer));
                            }
                            else
                            {
                                // no profiling, avoid the timer overhead
                                instruction.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.startPatching));
                            }
                            yield return instruction;
                        }
                        else
                        {
                            if (Prefs.LogVerbose)
                            {
                                CodeInstruction instruction = new CodeInstruction(OpCodes.Call);
                                instruction.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.endPatchingWithTimer));
                                yield return instruction;
                            }
                        }
                    }
                    else
                    {
                        yield return iList[i];
                    }
                }
            }
        }
    }
}
