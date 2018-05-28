using System.Reflection;
using Verse;
using System.IO;
using System.Diagnostics;
using System;
using System.Collections.Generic;

//
// file to test if the curent DLL is the newest version of the DLL of the name in question
// error if that is not the case
// warns about outdated versions
// Useful for keeping addon DLL files (like ModCheck) up to date in multiple mods
//
// Note: assumes AssemblyVersion and AssemblyFileVersion to be the same
// also the versions needs to be 4 digits, like 1.0.0.0
//

namespace ModCheck
{
    class VersionChecker
    {
        private class DLLinfo
        {
            public string mod;
            public Version version;
            public DLLinfo(string newMod, Version newVersion)
            {
                mod = newMod;
                version = newVersion;
            }
        }

        public static bool IsNewestVersion()
        {
            string mod = Assembly.GetExecutingAssembly().GetName().Name;
            string DLLname = mod + ".dll";
            string DLLfilename = Path.Combine("Assemblies", DLLname);
            List<DLLinfo> files = new List<DLLinfo>();
            Version maxVersion = new Version(0,0);
            Version firstVersion = null;

            // list versions of DLL files
            foreach (ModContentPack Pack in LoadedModManager.RunningModsListForReading)
            {
                string path = Path.Combine(Pack.RootDir, DLLfilename);
                string path2 = Path.Combine(GenFilePaths.CoreModsFolderPath, path);
                FileInfo dllFileInfo = new FileInfo(path2);
                if (dllFileInfo.Exists)
                {
                    FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(path2);
                    Version temp = new Version(myFileVersionInfo.FileVersion);
                    files.Add(new DLLinfo(Pack.Name, temp));
                    if (firstVersion == null)
                    {
                        firstVersion = temp;
                    }
                    if (temp.CompareTo(maxVersion) > 0)
                    {
                        maxVersion = temp;
                    }
                }
            }

            Version DLLVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (DLLVersion.CompareTo(maxVersion) < 0)
            {
                // only the newest version should print output to prevent each version from printing the same warnings. Once should be enough
                return false;
            }

            if (firstVersion.CompareTo(maxVersion) < 0)
            {
                Log.Error("[" + mod + "] using outdated " + DLLname + ". Update all outdated " + DLLname + " files and/or load " + mod + " mod first in modlist");
            }

            foreach (DLLinfo loopItem in files)
            {
                if (loopItem.version.CompareTo(maxVersion) < 0)
                {
                    Log.Warning("[" + loopItem.mod +"] Using outdated " + DLLname);
                }
            }

            return true;
        }
    }
}
