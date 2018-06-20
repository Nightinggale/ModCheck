using Verse;
using System.Collections.Generic;
using System.Diagnostics;

// this file contains a singleton, which acts as memory storage for ModCheck
// this allows memory, which isn't linked to a specific PatchOperation, like the cache system
// 

namespace ModCheck
{
    public sealed class Memory
    {
        private static Memory instance = new Memory();

        private List<string> patchOwners = new List<string>();
        private List<string> patchNames = new List<string>();
        private List<long> timeSpend = new List<long>();
        private Dictionary<string, int> modIndex = new Dictionary<string, int>();

        private int currentPatch;
        private Stopwatch stopWatch = Stopwatch.StartNew();

        private string currentModName = "";
        private string currentFileName = "";

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Memory()
        {
        }

        private Memory()
        {
        }

        public static Memory Instance
        {
            get
            {
                return instance;
            }
        }

        // setup called from HarmonyStarter
        public void init()
        {
            int counter = 0;
            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                modIndex[mod.Name] = counter;
                ++counter;
                foreach (PatchOperation patch in mod.Patches)
                {
                    try
                    {
                        ModCheckNameClass temp = patch as ModCheckNameClass;
                        patchNames.Add(temp.getPatchName());
                    }
                    catch
                    {
                        patchNames.Add("");
                    }
                    patchOwners.Add(mod.Name);
                    timeSpend.Add(0);
                }
            }
        }

        // calls from Harmony injected methods
        public void resetModAndFile()
        {
            currentModName = "";
            currentFileName = "";
        }

        public void setCurrentModName(string name)
        {
            currentModName = name;
        }

        public static void setCurrentFileName(LoadableXmlAsset current)
        {
            Instance.currentFileName = current.name;
            Instance.currentPatch = -1;
        }

        public static void startPatching()
        {
            ++Instance.currentPatch;
        }

        public static void startPatchingWithTimer()
        {
            ++Instance.currentPatch;
            Instance.stopWatch.Reset();
            Instance.stopWatch.Start();
        }

        public static void endPatchingWithTimer()
        {
            Instance.stopWatch.Stop();
            Instance.timeSpend[Instance.currentPatch] += Instance.stopWatch.ElapsedTicks;
        }

        // public access to contents, used by PatchOperations

        public static string getCurrentModName()
        {
            return Instance.currentModName;
        }

        public static string getCurrentFileName()
        {
            return Instance.currentFileName;
        }

        public static string getCurrentPatchOwner()
        {
            return Instance.patchOwners[Instance.currentPatch];
        }

        public static string getCurrentPatchName()
        {
            return Instance.patchNames[Instance.currentPatch];
        }

        public static bool isModLoaded(string name)
        {
            return Instance.modIndex.ContainsKey(name);
        }

        public static int getModLoadIndex(string name)
        {
            try
            {
                Instance.modIndex.TryGetValue(name, out int index);
                return index;
            }
            catch
            {
                return -1;
            }

        }


        // print to log and free memory
        public static void Clear()
        {
            if (instance == null)
            {
                return;
            }
            
            if (Prefs.LogVerbose)
            {
                int max = Instance.patchOwners.Count;
                if (max > 0) // having 0 patches is very unlikely, but it's best to support it anyway
                {
                    // print profiling results to the log

                    string lastMod = "";
                    long totalTime = 0;
                    List<string> modOutput = new List<string>();

                    for (int i = 0; i < max; ++i)
                    {
                        if (lastMod != Instance.patchOwners[i])
                        {
                            // current patch operation has a different owner than the last one, meaning it's the first operation in a new mod

                            lastMod = Instance.patchOwners[i];
                            long total = 0;
                            string patches = "";
                            // loop though all operations in the mod in question
                            for (int j = i; j < max; ++j)
                            {
                                if (lastMod != Instance.patchOwners[j])
                                {
                                    // new owner means no more operations in this mod
                                    break;
                                }
                                long timeSpendHere = Instance.timeSpend[j];
                                // accumulate total mod time
                                total += timeSpendHere;
                                // store the line generated for this operation in a temp string
                                patches += ("\n         " + ((timeSpendHere * 1000f) / Stopwatch.Frequency).ToString("F4").PadLeft(10) + " ms   " + Instance.patchNames[j]);
                            }
                            // all operations for the mod in question have been added
                            totalTime += total;
                            // print the mod total
                            string modLine = ("   " + ((total * 1000f)/ Stopwatch.Frequency).ToString("F4").PadLeft(10) + " ms " + lastMod);
                            // print the already generated lines for each operation
                            modOutput.Add(modLine + patches);
                        }
                    }
                    Log.Message("[ModCheck] Total time spent patching: " + ((totalTime* 1000f)/ Stopwatch.Frequency).ToString("F4").PadLeft(10) + " ms\nTime spent on each patch:");
                    foreach (string loopMod in modOutput)
                    {
                        Log.Message(loopMod);
                    }
                }
            }

            // clear the memory now that it's no longer needed
            instance = null;
        }
    }
}
