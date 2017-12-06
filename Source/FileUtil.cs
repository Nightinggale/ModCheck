using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// Copied with permission from ModSync Ninja
// Author: historic_os
// https://ludeon.com/forums/index.php?topic=34447.0

namespace RimWorld_ModSyncNinja
{
    class FileUtil
    {

        public static string GetModSyncVersionForMod(DirectoryInfo rootDir)
        {
            DirectoryInfo aboutDir = rootDir.GetDirectories("About").FirstOrDefault();
            if(aboutDir == null) return String.Empty;

            FileInfo modSyncXmlFile = aboutDir.GetFiles("ModSync.xml").FirstOrDefault();
            if(modSyncXmlFile == null) return String.Empty;
            try
            {
                FileStream fs = modSyncXmlFile.OpenRead();
                if (!fs.CanRead) return String.Empty;
                using (StreamReader sr = new StreamReader(fs))
                {
                    string content = sr.ReadToEnd();
                    return GetVersionFromModSyncXML(content);
                }

            }
#pragma warning disable CS0168
            catch (Exception e)
            {
                return String.Empty;
            }


        }

        private static string GetVersionFromModSyncXML(string fileContent)
        {
            var xml = XDocument.Parse(fileContent);
            if (!xml.Elements().Any())
            {
                throw new Exception("Invalid markup, null elements");
            }
            var root = xml.Element("ModSyncNinjaData");
            if (root == null)
            {
                throw new Exception("Invalid markup, missing root");
            }

            var xElement = root.Element("Version");
            if (xElement != null)
            {
                return xElement.Value;
            }
            return String.Empty;
        }
    }
}