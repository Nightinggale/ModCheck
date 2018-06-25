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
            DeepProfiler.Start("Applying Patches");

            // setup table of patch ownership
            if (!Memory.Instance.init())
            {
                // failure tells no patches were found
                // if this is the case, return false to avoid the index crash related to iterting an empty list in ApplyPatches
                return false;
            }

            // Blank the current mod/file since it's not present for vanilla patching.
            Memory.Instance.setModAndFile("", "", true);
            return true;
        }

        static void Postfix()
        {
            DeepProfiler.End();
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
                            if (Memory.profilingEnabled)
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
                            if (Memory.profilingEnabled)
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


    [HarmonyPatch(typeof(Verse.LoadedModManager))]
    [HarmonyPatch("LoadModXML")]
    class ModCheckPatching
    {
        [HarmonyPostfix]
        public static void AddModCheckPatching(List<LoadableXmlAsset> __result)
        {

            DeepProfiler.Start("Loading ModCheckPatches");

            Memory.Instance.LoadModCheckPatches();

            if (Memory.Instance.getModCheckPatches().Count > 0)
            {

                int iLength = __result.Count;
                Memory.Instance.resetPatchCount();

                foreach (PatchOperation current2 in Memory.Instance.getModCheckPatches())
                {
                    // always measure time. The cost overhead is less than measuring conditionally.
                    Memory.startPatchingWithTimer();
                    for (int i = 0; i < iLength; ++i)
                    {
                        Memory.Instance.setModAndFile(__result[i].mod.Name, __result[i].name, false);
                        current2.Apply(__result[i].xmlDoc);
                    }
                    Memory.endPatchingWithTimer();
                }

            }
            DeepProfiler.End();
        }
    }
}
