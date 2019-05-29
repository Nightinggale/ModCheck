using Harmony;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using System.Reflection.Emit;
using System.Xml;
using System.Text;

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
        static bool Prefix(Dictionary<XmlNode, LoadableXmlAsset> assetlookup)
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
            Memory.Instance.setassetlookup(assetlookup);
            return true;
        }

        static void Postfix()
        {
            DeepProfiler.End();
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> iList = new List<CodeInstruction>(instructions);

#if false
            // debug output
            // This will generate the assumedMethod filling code, complete with indexes for each line
            {
                string output = "";
                for (int i = 0; i < iList.Count; ++i)
                {
                    string code = iList[i].ToString();
                    code = code.Replace("\"", "\\\"");

                    output += "\n // ";
                    if (i < 10) output += " ";
                    output += i.ToString();
                    output += "\nassumedMethod.Add(\"" + code + "\");";
                }
                Log.Warning(output);
            }
#endif

            // first step is to verify that vanilla didn't change
            List<string> assumedMethod = new List<string>();

            //  0
            assumedMethod.Add("ldsfld System.Collections.Generic.List`1[Verse.ModContentPack] runningMods");
            //  1
            assumedMethod.Add("ldsfld System.Func`2[Verse.ModContentPack,System.Collections.Generic.IEnumerable`1[Verse.PatchOperation]] <>f__am$cache0");
            //  2
            assumedMethod.Add("brtrue Label1");
            //  3
            assumedMethod.Add("ldnull NULL");
            //  4
            assumedMethod.Add("ldftn IEnumerable`1 <ApplyPatches>m__0(Verse.ModContentPack)");
            //  5
            assumedMethod.Add("newobj Void .ctor(Object, IntPtr)");
            //  6
            assumedMethod.Add("stsfld System.Func`2[Verse.ModContentPack,System.Collections.Generic.IEnumerable`1[Verse.PatchOperation]] <>f__am$cache0");
            //  7
            assumedMethod.Add("ldsfld System.Func`2[Verse.ModContentPack,System.Collections.Generic.IEnumerable`1[Verse.PatchOperation]] <>f__am$cache0 [Label1]");
            //  8
            assumedMethod.Add("call IEnumerable`1 SelectMany[ModContentPack,PatchOperation](IEnumerable`1, System.Func`2[Verse.ModContentPack,System.Collections.Generic.IEnumerable`1[Verse.PatchOperation]])");
            //  9
            assumedMethod.Add("callvirt IEnumerator`1 GetEnumerator()");
            // 10
            assumedMethod.Add("stloc.1 NULL");
            // 11
            assumedMethod.Add("br Label2 [EX_BeginException]");
            // 12
            assumedMethod.Add("ldloc.1 NULL [Label5]");
            // 13
            assumedMethod.Add("callvirt Verse.PatchOperation get_Current()");
            // 14
            assumedMethod.Add("stloc.0 NULL");
            // 15
            assumedMethod.Add("ldloc.0 NULL [EX_BeginException]");
            // 16
            assumedMethod.Add("ldarg.0 NULL");
            // 17
            assumedMethod.Add("callvirt Boolean Apply(System.Xml.XmlDocument)");
            // 18
            assumedMethod.Add("pop NULL");
            // 19
            assumedMethod.Add("leave Label3");
            // 20
            assumedMethod.Add("stloc.2 NULL [EX_BeginCatch]");
            // 21
            assumedMethod.Add("ldstr \"Error in patch.Apply(): \"");
            // 22
            assumedMethod.Add("ldloc.2 NULL");
            // 23
            assumedMethod.Add("call System.String Concat(System.Object, System.Object)");
            // 24
            assumedMethod.Add("ldc.i4.0 NULL");
            // 25
            assumedMethod.Add("call Void Error(System.String, Boolean)");
            // 26
            assumedMethod.Add("leave Label4 [EX_EndException]");
            // 27
            assumedMethod.Add("ldloc.1 NULL [Label2, Label3, Label4]");
            // 28
            assumedMethod.Add("callvirt Boolean MoveNext()");
            // 29
            assumedMethod.Add("brtrue Label5");
            // 30
            assumedMethod.Add("leave Label6");
            // 31
            assumedMethod.Add("ldloc.1 NULL [EX_BeginFinally]");
            // 32
            assumedMethod.Add("brfalse Label7");
            // 33
            assumedMethod.Add("ldloc.1 NULL");
            // 34
            assumedMethod.Add("callvirt Void Dispose()");
            // 35
            assumedMethod.Add("endfinally NULL [Label7, EX_EndException]");
            // 36
            assumedMethod.Add("ret NULL [Label6]");


            // test if the loaded method is identical to the expected method
            bool bFoundCorrectMethod = true;

            if (iList.Count != assumedMethod.Count)
            {
                bFoundCorrectMethod = false;
            }

            // verify that each line is the same
            for (int i = 0; i < iList.Count && bFoundCorrectMethod; ++i)
            {
                if (iList[i].ToString() != assumedMethod[i])
                {
                    // line mismatch. Test that it isn't something silly like f__am$cache0 != f__am$cache2
                    bool match = false;
                    int offset = -1;

                    // lines with a known changing number
                    switch (i)
                    {
                        case 1:
                        case 6:
                        case 7:
                            offset = 119;
                            break;
                        case 4:
                            offset = 37;
                            break;
                    }

                    if (offset != -1)
                    {
                        try
                        {
                            // the mismatch could be a number in a generated var name
                            // replace the char in question with the new one
                            if (assumedMethod[i].Length > offset && assumedMethod[i].Length == iList[i].ToString().Length)
                            {
                                StringBuilder sb = new StringBuilder(assumedMethod[i]);
                                sb[offset] = iList[i].ToString().ToCharArray()[offset];

                                // make the comparison again, this time ignoring the char, which can modify itself
                                match = sb.ToString() == iList[i].ToString();
                            }
                        }
                        catch
                        {
                            // possibly unneeded try-catch. However it's here just to be safe. We really don't want to kill vanilla xml patching.
                        }
                    }

                    if (!match)
                    {
                        // still no match. The source is indeed modified.
                        bFoundCorrectMethod = false;
                    }
                    
                }
            }

            if (!bFoundCorrectMethod)
            {
                Log.Error("[ModCheck] Internal failure patching Verse.LoadedModManager.ApplyPatches");
                for (int i = 0; i < iList.Count; ++i)
                {
                    // return the unmodified vanilla code
                    yield return iList[i];
                }
            }
            else
            {
                // keep the method intact, but add calls to Memory to tell when patching is started or stopped.
                for (int i = 0; i < iList.Count; ++i)
                {
                    // using indexes is perfectly safe because it works as long as vanilla is unchanged and that's already verified at this point
                    if (i == 15)
                    {
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
                    else if (i == 19)
                    {
                        if (Memory.profilingEnabled)
                        {
                            CodeInstruction instruction = new CodeInstruction(OpCodes.Call);
                            instruction.operand = typeof(ModCheck.Memory).GetMethod(nameof(ModCheck.Memory.endPatchingWithTimer));
                            yield return instruction;
                        }
                    }
                    yield return iList[i];
                }
            }
        }
    }


#if false
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
#endif
}
