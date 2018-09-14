using Verse;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Linq;

// this file contains a singleton, which acts as memory storage for ModCheck
// this allows memory, which isn't linked to a specific PatchOperation, like the cache system
// 

namespace ModCheck
{
    public sealed class Memory
    {
        private class PatchMemoryModule
        {
            int modIndex;
            string patchOwner;
            string patchName;
            private Stopwatch stopWatch = Stopwatch.StartNew();
            private string folderString;

            public PatchMemoryModule(int index, string owner, PatchOperation patch, bool isModCheckPatch)
            {
                modIndex = index;
                patchOwner = owner;
                folderString = isModCheckPatch ? "M " : "P ";
                stopWatch.Reset();
                try
                {
                    ModCheckNameClass temp = patch as ModCheckNameClass;
                    patchName = temp.getPatchName();
                }
                catch
                {
                    patchName = "";
                }
            }

            public void start()
            {
                stopWatch.Start();
            }

            public void stop()
            {
                stopWatch.Stop();
            }

            public long getTime
            {
                get { return stopWatch.ElapsedTicks; }
            }

            public string getName
            {
                get { return patchName; }
            }

            public string getOwner
            {
               get { return patchOwner; }
            }

            public int getIndex
            {
                get { return modIndex; }
            }

            public string getFolderString
            {
                get { return folderString; }
            }
        }

        private static Memory instance = new Memory();
        private Dictionary<string, int> modIndex = new Dictionary<string, int>();

        private int currentPatch;
        private string currentModName = "";
        private string currentFileName = "";


        private List<PatchMemoryModule> PatchMemory = new List<PatchMemoryModule>();
        private List<PatchMemoryModule> VanillaMemory = new List<PatchMemoryModule>();
        private bool workingOnModCheckPatches = true;


        private List<PatchOperation> patches = new List<PatchOperation>();

        private Dictionary<XmlNode, LoadableXmlAsset> assetlookupMemory;

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

        private PatchMemoryModule getModule()
        {
            if (workingOnModCheckPatches)
            {
                return PatchMemory[currentPatch];
            }
            else
            {
                return VanillaMemory[currentPatch];
            }
        }

        // setup called from HarmonyStarter
        public bool init()
        {
            int counter = 0;
            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                modIndex[mod.Name] = counter;
                foreach (PatchOperation patch in mod.Patches)
                {
                    VanillaMemory.Add(new PatchMemoryModule(counter, mod.Name, patch, false));
                }
                ++counter;
            }
            return VanillaMemory.Count > 0;
        }

        public static bool profilingEnabled
        {
            get { return Prefs.LogVerbose; }
        }

        public void setModAndFile(string modName, string fileName, bool resetPatches)
        {
            currentModName = modName;
            currentFileName = fileName;
            if (resetPatches)
            {
                resetPatchCount();
                workingOnModCheckPatches = false;
            }
        }

        public void resetPatchCount()
        {
            Instance.currentPatch = -1;
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

        public void setassetlookup(Dictionary<XmlNode, LoadableXmlAsset> assetlookup)
        {
            this.assetlookupMemory = assetlookup;
        }

        public static Dictionary<XmlNode, LoadableXmlAsset> getXmlAssets()
        {
            return Instance.assetlookupMemory;
        }

        public static void startPatching()
        {
            ++Instance.currentPatch;
        }

        public static void startPatchingWithTimer()
        {
            ++Instance.currentPatch;
            Instance.getModule().start();
        }

        public static void endPatchingWithTimer()
        {
            Instance.getModule().stop();
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
            return Instance.getModule().getOwner;
        }

        public static string getCurrentPatchName()
        {
            return Instance.getModule().getName;
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

        private IEnumerable<PatchMemoryModule> getModulesInOrder()
        {
            int patchMax = PatchMemory.Count;
            int vanillaMax = VanillaMemory.Count;
            int patchCounter = 0;
            int vanillaCounter = 0;

            while (patchMax > patchCounter && vanillaMax > vanillaCounter)
            {
                if (PatchMemory[patchCounter].getIndex <= VanillaMemory[vanillaCounter].getIndex)
                {
                    yield return PatchMemory[patchCounter];
                    ++patchCounter;
                }
                else
                {
                    yield return VanillaMemory[vanillaCounter];
                    ++vanillaCounter;
                }
            }
            for (; patchMax > patchCounter; ++patchCounter)
            {
                yield return PatchMemory[patchCounter];
            }
            for (; vanillaMax > vanillaCounter; ++vanillaCounter)
            {
                yield return VanillaMemory[vanillaCounter];
            }
        }

        private IEnumerable<List<PatchMemoryModule>> getModulesInMods()
        {
            List<PatchMemoryModule> list = new List<PatchMemoryModule>();
            int currentMod = -1;
            foreach (PatchMemoryModule module in getModulesInOrder())
            {
                if (module.getIndex != currentMod)
                {
                    if (list.Count > 0)
                    {
                        yield return list;
                    }
                    list.Clear();
                    currentMod = module.getIndex;
                }

                list.Add(module);
            }
            if (list.Count > 0)
            {
                yield return list;
            }
        }


        // print to log and free memory
        public static void Clear()
        {
            if (instance == null)
            {
                return;
            }


            if (Memory.profilingEnabled)
            {
                long totalTime = 0;
                List<string> modOutput = new List<string>();
                foreach (List<PatchMemoryModule> list in instance.getModulesInMods())
                {
                    string output = "";
                    long time = 0;
                    foreach (PatchMemoryModule module in list)
                    {
                        long timeSpendHere = module.getTime;
                        time += timeSpendHere;
                        output += ("\n         " + ((timeSpendHere * 1000f) / Stopwatch.Frequency).ToString("F4").PadLeft(10) + " ms   " + module.getFolderString + module.getName);
                    }

                    totalTime += time;
                    string modLine = ("   " + ((time * 1000f) / Stopwatch.Frequency).ToString("F4").PadLeft(10) + " ms " + list[0].getOwner);

                    modOutput.Add(modLine + output);
                }
                Log.Message("[ModCheck] Total time spent patching: " + ((totalTime * 1000f) / Stopwatch.Frequency).ToString("F4").PadLeft(10) + " ms\nTime spent on each patch:");
                foreach (string loopMod in modOutput)
                {
                    Log.Message(loopMod);
                }
            }

            // clear the memory now that it's no longer needed
            instance = null;
        }

        public List<PatchOperation> getModCheckPatches()
        {
            return patches;
        }

        public void LoadModCheckPatches()
        {

            DeepProfiler.Start("Loading all ModCheck patches");
            this.patches = new List<PatchOperation>();
            int modIndex = -1;
            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                ++modIndex;
                List<LoadableXmlAsset> list = DirectXmlLoader.XmlAssetsInModFolder(mod, "ModCheckPatches/").ToList<LoadableXmlAsset>();
                for (int i = 0; i < list.Count; i++)
                {
                    XmlElement documentElement = list[i].xmlDoc.DocumentElement;
                    if (documentElement.Name != "Patch")
                    {
                        Log.Error(string.Format("Unexpected document element in patch XML; got {0}, expected 'Patch'", documentElement.Name), false);
                    }
                    else
                    {
                        for (int j = 0; j < documentElement.ChildNodes.Count; j++)
                        {
                            XmlNode xmlNode = documentElement.ChildNodes[j];
                            if (xmlNode.NodeType == XmlNodeType.Element)
                            {
                                if (xmlNode.Name != "Operation")
                                {
                                    Log.Error(string.Format("Unexpected element in patch XML; got {0}, expected 'Operation'", documentElement.ChildNodes[j].Name), false);
                                }
                                else
                                {
                                    PatchOperation patchOperation = DirectXmlToObject.ObjectFromXml<PatchOperation>(xmlNode, false);
                                    patchOperation.sourceFile = list[i].FullFilePath;
                                    this.patches.Add(patchOperation);
                                    PatchMemory.Add(new PatchMemoryModule(modIndex, mod.Name, patchOperation, true));
                                }
                            }
                        }
                    }
                }
            }
            DeepProfiler.End();
        }
    }
}
