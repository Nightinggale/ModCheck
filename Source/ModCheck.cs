﻿using System.Linq;
using System.Xml;
using Verse;
using System;
using RimWorld;

namespace ModCheck
{
#pragma warning disable CS0649

    public abstract class ModCheckBase : PatchOperation
    {
        protected string modName;
        protected string yourMod;
        protected bool errorOnFail = false;

        protected string customMessageSuccess;
        protected string customMessageFail;

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

        protected virtual string getDefaultErrorString()
        {
            Log.Error("Base getDefaultErrorString() called");
            return null;
        }

        private void writeLogEntry(bool testPassed)
        {
            string messageStr = null;

            if (testPassed)
            {
                if (!customMessageSuccess.NullOrEmpty())
                {
                    messageStr = customMessageSuccess;
                }
            }
            else
            {
                if (!customMessageFail.NullOrEmpty())
                {
                    messageStr = customMessageFail;
                }
                if (errorOnFail)
                {
                    if (messageStr.NullOrEmpty())
                    {
                        messageStr = getDefaultErrorString();
                    }
                }
            }

            if (!messageStr.NullOrEmpty())
            {
                if (!testPassed && errorOnFail)
                {
                    Log.Error(messageStr);
                }
                else
                {
                    Log.Message(messageStr);
                }
            }
        }

        protected virtual void handleError(ArgumentException ex)
        {
            if (ex.Message == "MissingModName")
            {
                Log.Error("ModCheck: used with missing or empty modName");
            }
            else if (ex.Message == "MissingYourMod")
            {
                Log.Error("ModCheck: used with missing or empty yourMod");
            }
        }

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (modName.NullOrEmpty()) throw new ArgumentException("MissingModName");
                if (yourMod.NullOrEmpty()) throw new ArgumentException("MissingYourMod");

                bool testPassed = isTestPassed();
                writeLogEntry(testPassed);
                return testPassed;
            }
            catch (ArgumentException ex)
            {
                handleError(ex);
                return false;
            }
        }
    }

    // tells if a mod is loaded
    // returns true if it is and false if not
    // setting incompatible to true will invert the output and change error messages
    public class isModLoaded : ModCheckBase
    {
        private bool incompatible = false;

        protected override bool isTestPassed()
        {
            bool modLoaded = isModLoaded(modName);

            return (modLoaded && !incompatible) || (!modLoaded && incompatible);
        }

        protected override string getDefaultErrorString()
        {
            if (incompatible)
            {
                return "Incompatible mods in use: \"" + modName + "\" can't be used with \"" + yourMod + "\"";
            }
            return "Missing mod: \"" + modName + "\", needed by \"" + yourMod + "\"";
        }
    }

    public class loadOrder : ModCheckBase
    {
        private bool yourModFirst = false;

        protected override bool isTestPassed()
        {
            if (yourModFirst)
            {
                return isModLoadedBeforeMod(yourMod, modName);
            }
            return isModLoadedBeforeMod(modName, yourMod);
        }

        protected override string getDefaultErrorString()
        {
            if (yourModFirst)
            {
                return "Mod load order: \"" + yourMod + "\" needs to be loaded before \"" + modName + "\"";
            }
            return "Mod load order: \"" + modName + "\" needs to be loaded before \"" + yourMod + "\"";
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
    
    // check that the given version is the same or lower than the version read from the mod
    public class isVersion : ModCheckBase
    {
        private string version;

        protected override bool isTestPassed()
        {
            Version min = getVersion(version, "MinVersionUnreadable");
            int index = getModLoadIndex(modName);
            if (index != -1)
            {
                string currentStr = ModsConfig.ActiveModsInLoadOrder.ElementAt(index).TargetVersion;
                Version current = getVersion(currentStr, "CurrentVersionUnreadable");
                return current.Major >= min.Major && current.Minor >= min.Minor && current.Build >= min.Build;
            }
            return true;
        }

        protected override string getDefaultErrorString()
        {
            int index = getModLoadIndex(modName);
            string readVersion = ModsConfig.ActiveModsInLoadOrder.ElementAt(index).TargetVersion;
            return yourMod + " requires " + modName + " " + version + " but version " + readVersion + " is used";
        }

        private Version getVersion(string versionString, string exceptionType)
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
                Log.Error("ModCheck.isVersion used with an invalid string. Needs the format int.int.int (gameengine has the same requirement)");
            }
            else if (ex.Message == "CurrentVersionUnreadable")
            {
                Log.Error("ModCheck.isVersion used with an invalid string. Needs the format int.int.int (gameengine has the same requirement)");
            }
            else
            {
                base.handleError(ex);
            }
        }
    }
    
}
