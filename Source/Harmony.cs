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
            var harmony = HarmonyInstance.Create("com.rimworld.modcheck");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // setup table of patch ownership
            Memory.Instance.init();

            // check DLL version of ModCheck to ensure the newest is in use
            VersionChecker.CheckDLLVersion();
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

    [HarmonyPatch(typeof(Verse.ModContentPack))]
    [HarmonyPatch("LoadDefs")]
    class LoadAllMods
    {
        static bool Prefix(ModContentPack __instance)
        {
            // Update which mod is being worked on
            Memory.Instance.setCurrentModName(__instance.Name);
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> iList = new List<CodeInstruction>(instructions);

            if (iList.Count != 171)
            {
                // already patched. Do nothing
                foreach (CodeInstruction Instruction in (instructions))
                {
                    yield return Instruction;
                }
            }
            else
            {
                // no change to the vanilla code
                // all what is done here is to add calls to void methods to allow ModCheck to read contents of variables
                // all injections are carefully placed in locations where the stack is empty and they leave the stack empty
                // this should make the injections invisible to the surrounding code

                /*
                 * vanilla code with points of interested pointed out
                 * 
                 * 
                 * 	foreach (LoadableXmlAsset current in list)
	             *  {
                 *  // Injection point A
	             *     	foreach (PatchOperation current2 in patches)
	             *     	{
                 *     	    // Injection point B
	             *     		current2.Apply(current.xmlDoc);
                 *     		// injection point C
	             *     	}
                 *	}
                 */

                // TODO add some checks that selected instructions have the assumed upcodes

                for (int i = 0; i < 17; ++i)
                {
                    yield return iList[i];
                }

                // Injection point A

                // tell ModCheck the name of the current file

                // copy LoadableXmlAsset current to stack
                yield return new CodeInstruction(OpCodes.Ldloc_1);

                // call ModCheck to set the filename from the stack
                CodeInstruction instruction1 = new CodeInstruction(OpCodes.Call);
                instruction1.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.setCurrentFileName));
                yield return instruction1;


                for (int i = 17; i < 21; ++i)
                {
                    yield return iList[i];
                }

                // Injection point B

                // Inform ModCheck that the next patch has been reached
                // no arguments as it's just incrementing a counter
                CodeInstruction instruction2 = new CodeInstruction(OpCodes.Call);
                if (Prefs.LogVerbose)
                {
                    // start the timer as well as incrementing the counter
                    instruction2.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.startPatchingWithTimer));
                }
                else
                {
                    // no profiling, avoid the timer overhead
                    instruction2.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.startPatching));
                }
                yield return instruction2;

                for (int i = 21; i < 26; ++i)
                {
                    yield return iList[i];
                }

                // injection point C

                // stop the timer and store the result, but only if the timer is in use
                if (Prefs.LogVerbose)
                {
                    CodeInstruction instruction3 = new CodeInstruction(OpCodes.Call);
                    instruction3.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.endPatchingWithTimer));
                    yield return instruction3;
                }

                for (int i = 26; i < 171; ++i)
                {
                    yield return iList[i];
                }
            }
        }
    }
}
