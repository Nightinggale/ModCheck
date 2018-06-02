using System.Linq;
using System.Xml;
using Verse;
using System;
using System.Collections.Generic;
using RimWorld;

namespace ModCheck
{
#pragma warning disable CS0649

    public abstract class ModCheckBase : ModCheckLog
    {
        protected string modName;
        protected string yourMod;

        protected List<string> altModNames = new List<string>();

        private bool internalHasCache = false;
        private bool internalSuccess  = false;


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

        protected virtual bool isTestPassed()
        {
            Log.Error("Base isTestPassed() called");
            return false;
        }

        protected virtual void handleError(ArgumentException ex)
        {
            if (ex.Message == "MissingModName")
            {
                printString("{2} {3} contains operation {4} with missing modName", Seriousness.Error);
            }
        }

        protected override bool ApplyWorker(XmlDocument xml)
        {
            // use cached output if it exist
            // the reason is that the calculations should be done once, not once for each def xml file
            if (internalHasCache)
            {
                return internalSuccess;
            }
            internalHasCache = true;
            
            try
            {
                // include the modname in the alt names. That way all names will be used if alt names are looped
                if (!modName.NullOrEmpty() && !altModNames.Contains(modName))
                {
                    altModNames.Add(modName);
                }

                internalSuccess = isTestPassed();
                printLogMessages(internalSuccess);
                return internalSuccess;
            }
            catch (ArgumentException ex)
            {
                handleError(ex);
                internalSuccess = false;
                return false;
            }
        }

        public override void resetRun()
        {
            internalHasCache = false;
        }
    }

    // tells if a mod is loaded
    // returns true if it is and false if not
    // setting incompatible to true will invert the output and change error messages
    public class IsModLoaded : isModLoaded { }
    public class isModLoaded : ModCheckBase
    {
        private bool incompatible = false;

        protected override bool isTestPassed()
        {
            bool modLoaded = false;
            if (modName.NullOrEmpty()) throw new ArgumentException("MissingModName");

            foreach (string loopname in altModNames)
            {
                modLoaded = Memory.isModLoaded(loopname);
                if (modLoaded) break;
            }

            return (modLoaded && !incompatible) || (!modLoaded && incompatible);
        }

        protected override string getDefaultErrorString()
        {
            if (incompatible)
            {
                return "Incompatible mods in use: \"" + modName + "\" can't be used with \"{2}\"";
            }
            return "Missing mod: \"" + modName + "\", needed by \"{2}\"";
        }
    }

    public class LoadOrder : loadOrder { }
    public class loadOrder : ModCheckBase
    {
        private bool yourModFirst = false;
        private string first;
        private string last;

        protected override bool isTestPassed()
        {
            if (!first.NullOrEmpty() || !last.NullOrEmpty())
            {
                if (yourModFirst || !yourMod.NullOrEmpty() || !modName.NullOrEmpty() || altModNames.Count > 0)
                {
                    throw new ArgumentException("MixedMode");
                }

                // insert the current patch owner if one field is empty
                if (first.NullOrEmpty())
                {
                    first = Memory.getCurrentPatchOwner();
                }
                else if (last.NullOrEmpty())
                {
                    last = Memory.getCurrentPatchOwner();
                }
                return isModLoadedBeforeMod(first, last);
            }

            if (yourMod.NullOrEmpty())
            {
                yourMod = Memory.getCurrentPatchOwner();
            }
            if (modName.NullOrEmpty()) throw new ArgumentException("MissingModName");
            foreach (string loopName in altModNames)
            {
                bool success = yourModFirst ? isModLoadedBeforeMod(yourMod, loopName) : isModLoadedBeforeMod(loopName, yourMod);
                if (!success)
                {
                    return false;
                }
            }
            return true;
        }

        protected override string getDefaultErrorString()
        {
            if (!first.NullOrEmpty())
            {
                return "Mod load order: " + first + " needs to be loaded before " + last;
            }

            if (yourModFirst)
            {
                return "Mod load order: \"{2}\" needs to be loaded before \"" + modName + "\"";
            }
            return "Mod load order: \"" + modName + "\" needs to be loaded before \"{2}\"";
        }

        protected bool isModLoadedBeforeMod(string first, string last)
        {
            int firstIndex = Memory.getModLoadIndex(first);
            if (firstIndex == -1)
            {
                return true;
            }
            int lastIndex = Memory.getModLoadIndex(last);
            return lastIndex == -1 || firstIndex < lastIndex;
        }

        protected override void handleError(ArgumentException ex)
        {
            if (ex.Message == "MixedMode")
            {
                printString("{2} {3} contains LoadOrder operation {4} with tags for both mode 1 and 2 (mixed mode not supported)", Seriousness.Error);
            }
            else if (ex.Message == "MissingModName")
            {
                printString("{2} {3} contains LoadOrder operation {4} in mode 2, which is missing modName", Seriousness.Error);
            }
            else
            {
                base.handleError(ex);
            }
        }
    }
    
    // check that the given version is the same or lower than the version read from the mod
    public class IsVersion : isVersion { }
    public class isVersion : ModCheckBase
    {
        private string version;

        protected override bool isTestPassed()
        {
            Version min = getVersion(version, "MinVersionUnreadable");
            if (modName.NullOrEmpty()) throw new ArgumentException("MissingModName");

            foreach (string loopName in altModNames)
            {
                int index = Memory.getModLoadIndex(loopName);
                if (index != -1)
                {
                    string currentStr = ModsConfig.ActiveModsInLoadOrder.ElementAt(index).TargetVersion;
                    Version current = getVersion(currentStr, "CurrentVersionUnreadable");
                    if (current.Major < min.Major || current.Minor < min.Minor || current.Build < min.Build)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override string getDefaultErrorString()
        {
            int index = Memory.getModLoadIndex(modName);
            string readVersion = ModsConfig.ActiveModsInLoadOrder.ElementAt(index).TargetVersion;
            return Memory.getCurrentPatchOwner() + " requires " + modName + " " + version + " but version " + readVersion + " is used";
        }

        protected Version getVersion(string versionString, string exceptionType)
        {

            if (versionString.NullOrEmpty() || !VersionControl.IsWellFormattedVersionString(versionString))
            {
                throw new ArgumentException(exceptionType);
            }
            try
            {
                return VersionControl.VersionFromString(versionString);
            }
            catch
            {
                throw new ArgumentException(exceptionType);
            }
        }
        

        protected override void handleError(ArgumentException ex)
        {
            if (ex.Message == "MinVersionUnreadable")
            {
                printString("{2} {3} contains isVersion operation {4} with invalid version tag. Should be 2-4 ints divided by periods (like 1.2.3)", Seriousness.Error);
            }
            else if (ex.Message == "CurrentVersionUnreadable")
            {
                printString("{2} {3} contains isVersion operation {4}, which read an ivalid version tag from another mod. Should be 2-4 ints divided by periods (like 1.2.3)", Seriousness.Error);
            }
            else
            {
                base.handleError(ex);
            }
        }
    }

    // check that the given version is the same or lower than the version read from the mod
    public class IsModSyncVersion : isModSyncVersion { }
    public class isModSyncVersion : ModCheckBase
    {
        private string version;

        protected override string getDefaultErrorString()
        {
            if (modName.NullOrEmpty()) throw new ArgumentException("MissingModName");
            int index = Memory.getModLoadIndex(modName);
            ModMetaData MetaData = ModsConfig.ActiveModsInLoadOrder.ElementAt(index);
            string ModVersion = RimWorld_ModSyncNinja.FileUtil.GetModSyncVersionForMod(MetaData.RootDir);
            return Memory.getCurrentPatchOwner() + " requires " + modName + " " + version + " but version " + ModVersion + " is used";
        }

        protected override bool isTestPassed()
        {
            int index = Memory.getModLoadIndex(modName);
            if (index != -1)
            {
                ModMetaData MetaData = ModsConfig.ActiveModsInLoadOrder.ElementAt(index);
                string ModVersion = RimWorld_ModSyncNinja.FileUtil.GetModSyncVersionForMod(MetaData.RootDir);

                Version current;
                try
                {
                    current = new Version(ModVersion);
                }
                catch
                {
                    throw new ArgumentException("CurrentVersionUnreadable");
                }

                Version min;
                try
                {
                    min = new Version(version);
                }
                catch
                {
                    throw new ArgumentException("MinVersionUnreadable");
                }

                return current.CompareTo(min) > -1;
            }

            return true;
        }

        protected override void handleError(ArgumentException ex)
        {
            if (ex.Message == "MinVersionUnreadable")
            {
                printString("{2} {3} contains isModSyncVersion operation {4} with invalid version tag. Should be 2-4 ints divided by periods (like 1.2.3)", Seriousness.Error);
            }
            else if (ex.Message == "CurrentVersionUnreadable")
            {
                printString("{2} {3} contains isModSyncVersion operation {4}, which read an ivalid version tag from another mod. Should be 2-4 ints divided by periods (like 1.2.3)", Seriousness.Error);
            }
            else
            {
                base.handleError(ex);
            }
        }
    }
}
