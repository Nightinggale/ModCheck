using System.Xml;
using Verse;
using System.Collections.Generic;

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

    public class Move : move { }
    public class move : ModCheckNameClass
    {
        protected string xpath;
        protected List<string> followers = new List<string>();

        protected override bool ApplyWorker(XmlDocument xml)
        {
            foreach (object firstObject in xml.SelectNodes(this.xpath))
            {
                XmlNode first = firstObject as XmlNode;
                if (first != null)
                {
                    LoadableXmlAsset loadableXmlAsset = Memory.getXmlAssets().TryGetValue(first, null);
                    bool result = false;
                    // loop from the rear end because this is FILO (first in, last out)
                    // each time an object is found, it's appended right after xpath
                    // this means the last found object will be first 
                    int length = followers.Count - 1;
                    for (int i = length; i >= 0; --i)
                    {
                        foreach (object currentObject in xml.SelectNodes(this.followers[i]))
                        {
                            XmlNode current = currentObject as XmlNode;
                            if (current != null)
                            {
                                result = true;
                                first.ParentNode.InsertAfter(current, first);
                                if (loadableXmlAsset != null)
                                {
                                    // assign a new LoadableXmlAsset for the moved element
                                    // this is needed if the element is moved to elements from another mod (Core is a mod in this context)
                                    Memory.getXmlAssets()[current] = loadableXmlAsset;
                                }
                            }
                        }
                    }
                    return result;
                }
            }

            return false;
        }
    }
}
