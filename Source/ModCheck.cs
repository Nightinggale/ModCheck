using System.Linq;
using System.Xml;
using Verse;
using System;

namespace ModCheck
{
#pragma warning disable CS0649

    public abstract class ModCheckBase : PatchOperation
    {
        protected string modName;
        protected string yourMod;
        protected bool errorOnFail = false;

        // check that the standard tags needed for all methods are set
        protected bool isMandatorySet(string thisMethod)
        {
            if (modName.NullOrEmpty())
            {
                Log.Error("modCheck." + thisMethod + " used with an empty/missing modName");
                return false;
            }
            if (yourMod.NullOrEmpty())
            {
                Log.Error("modCheck." + thisMethod + " used with an empty/missing yourMod");
                return false;
            }
            return true;
        }

        protected bool isModLoaded(string name)
        {
            return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == name);
        }

        protected int getModLoadIndex(string name)
        {
            int i = 0;
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod.Name == name)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        protected bool isModLoadedBeforeMod(string first, string last)
        {
            int firstIndex = getModLoadIndex(first);
            if (firstIndex == -1)
            {
                return true;
            }
            int lastIndex = getModLoadIndex(last);
            return lastIndex == -1 || firstIndex < lastIndex;
        }
    }

    // tells if a mod is loaded
    // returns true if it is and false if not
    // setting incompatible to true will invert the output and change error messages
    public class isModLoaded : ModCheckBase
    {
        private bool incompatible = false;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!isMandatorySet("isModLoaded"))
            {
                return false;
            }

            bool modLoaded = isModLoaded(modName);

            if (modLoaded && incompatible)
            {
                if (errorOnFail)
                {
                    Log.Error("Incompatible mods in use: \"" + modName + "\" can't be used with \"" + yourMod + "\"");
                }
                return false;
            }
            else if (!modLoaded && !incompatible)
            {
                if (errorOnFail)
                {
                    Log.Error("Missing mod: \"" + modName + "\", needed by \"" + yourMod + "\"");
                }
                return false;
            }

            return true;
        }
    }

    public class loadOrder : ModCheckBase
    {
        private bool yourModFirst = false;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!isMandatorySet("loadOrder"))
            {
                return false;
            }
            if (yourModFirst)
            {
                // reverse order
                string temp = modName;
                modName = yourMod;
                yourMod = temp;
            }

            if (isModLoadedBeforeMod(modName, yourMod))
            {
                return true;
            }
            if (errorOnFail)
            {
                Log.Error("Mod load order: \"" + modName + "\" needs to be loaded before \"" + yourMod + "\"");
            }
            return false;
        }
    }
    
    // check that the given version is the same or lower than the version read from the mod
    public class isVersion : ModCheckBase
    {
        private string version;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!isMandatorySet("isVersion"))
            {
                return false;
            }

            if (version.NullOrEmpty())
            {
                Log.Error("modCheck.isVersion used with an empty/missing version");
                return false;
            }

            int index = getModLoadIndex(modName);
            if (index != -1)
            {
                string readVersion = ModsConfig.ActiveModsInLoadOrder.ElementAt(index).TargetVersion;
                string[] versions = readVersion.Split('.');
                string[] wantedVersions = version.Split('.');
                if (versions.Count() != wantedVersions.Count())
                {
                    Log.Error("ModCheck.isVersion failed to compare version tags " + version + " and " + readVersion + " for mod " + modName + " checked in mod " + yourMod);
                    return false;
                }
                for (int i = 0; i < versions.Count(); i++)
                {
                    int readIndex = -1;
                    int wantedIndex = -1;

                    if (!Int32.TryParse(versions[i], out readIndex))
                    {
                        Log.Error("ModCheck.isVersion failed to understand version string " + readVersion + " while testing " + modName + " in " + yourMod);
                        return false;
                    }
                    if (!Int32.TryParse(wantedVersions[i], out wantedIndex))
                    {
                        Log.Error("ModCheck.isVersion failed to understand version string " + readVersion + " while testing in " + yourMod);
                        return false;
                    }
                    if (readIndex < wantedIndex)
                    {
                        if (errorOnFail)
                        {
                            Log.Error(yourMod + " requires " + modName + " " + wantedIndex + " but version " + readIndex + " is used");
                        }
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
    
}
