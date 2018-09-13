using System.Xml;
using Verse;

namespace ModCheck
{
#pragma warning disable CS0649

#if false
    public class FindFile : findFile { }
    public class findFile : ModCheckNameClass
    {
        private string modName;
        private string file;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!modName.NullOrEmpty())
            {
                if (modName != Memory.getCurrentModName())
                {
                    return false;
                }
            }
            if (!file.NullOrEmpty())
            {
                if (file != Memory.getCurrentFileName())
                {
                    return false;
                }
            }


            return true;
        }
    }
#endif
}
