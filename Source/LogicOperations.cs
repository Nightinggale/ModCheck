using System.Xml;
using Verse;
using System.Collections.Generic;

namespace ModCheck
{
#pragma warning disable CS0649

    public class ModCheckNameClass : PatchOperation
    {
        private string patchName;
        public string getPatchName()
        {
            if (patchName.NullOrEmpty())
            {
                return "";
            }
            return patchName;
        }

        public virtual void resetRun()
        {
        }
    }

    public class AND : ModCheckNameClass
    {
        private List<PatchOperation> tests;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            foreach (PatchOperation current in this.tests)
            {
                if (!current.Apply(xml))
                {
                    return false;
                }
            }
            return true;
        }

        public override void resetRun()
        {
            foreach (PatchOperation current in tests)
            {
                try
                {
                    ModCheckNameClass temp = current as ModCheckNameClass;
                    temp.resetRun();
                }
                catch { }
            }
        }
    }

    public class OR : ModCheckNameClass
    {
        private List<PatchOperation> tests;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            foreach (PatchOperation current in this.tests)
            {
                if (current.Apply(xml))
                {
                    return true;
                }
            }
            return false;
        }

        public override void resetRun()
        {
            foreach (PatchOperation current in tests)
            {
                try
                {
                    ModCheckNameClass temp = current as ModCheckNameClass;
                    temp.resetRun();
                }
                catch { }
            }
        }
    }

    public class IfElse : ifElse { }
    public class ifElse : ModCheckNameClass
    {
        private PatchOperation test;
        private PatchOperation passed;
        private PatchOperation failed;
        private bool passInnerTest = true;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool result = test.Apply(xml);
            bool inner;

            if (result)
            {
                inner = passed.Apply(xml);
            }
            else
            {
                inner = failed.Apply(xml);
            }
            if (passInnerTest)
            {
                return inner;
            }
            return result;
        }

        public override void resetRun()
        {
            try
            {
                ModCheckNameClass temp = test as ModCheckNameClass;
                temp.resetRun();
            }
            catch { }
            try
            {
                ModCheckNameClass temp = passed as ModCheckNameClass;
                temp.resetRun();
            }
            catch { }
            try
            {
                ModCheckNameClass temp = failed as ModCheckNameClass;
                temp.resetRun();
            }
            catch { }
        }
    }

    public class Once : once { }
    public class once : ModCheckNameClass
    {
        private bool executed = false;
        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (executed)
            {
                return false;
            }
            executed = true;
            return true;
        }
    }

    public class Sequence : sequence { }
    public class sequence : ModCheckNameClass
    {
        private List<PatchOperation> operations;
        private bool once = false;
        private bool stopOnFail = true;


        private bool executed = false;
        private bool returnValue = true;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (once && executed)
            {
                return false;
            }
            executed = true;

            foreach (PatchOperation current in operations)
            {
                if (!current.Apply(xml))
                {
                    returnValue = false;
                    if (stopOnFail) return false;
                }
            }
            return returnValue;
        }

        public override void resetRun()
        {
            foreach (PatchOperation current in operations)
            {
                try
                {
                    ModCheckNameClass temp = current as ModCheckNameClass;
                    temp.resetRun();
                }
                catch { }
            }
        }
    }

    public class Loop : loop { }
    public class loop : ModCheckNameClass
    {
        private int times = 1;
        private PatchOperation operation;
        private bool reset = true;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (operation == null)
            {
                Log.Error("[ModCheck] loop set without operation");
                return false;
            }

            for (int i = 0; i < times; ++i)
            {
                if (reset)
                {
                    try
                    {
                        ModCheckNameClass temp = operation as ModCheckNameClass;
                        temp.resetRun();
                    }
                    catch { }
                }
                operation.Apply(xml);
            }
            return true;
        }
    }

    public class Search : search { }
    public class search : ModCheckNameClass
    {
        protected string xpath;
        private bool stopOnFail = true;
        private List<PatchOperation> operations;
        protected string tag = "SearchResult";

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool result = false;
            XmlElement tmp = null;

            foreach (object current in xml.SelectNodes(this.xpath))
            {
                if (tmp == null)
                {
                    tmp = xml.CreateElement(tag);
                    xml.DocumentElement.AppendChild(tmp);
                    result = true;
                }
                XmlNode xmlNode = current as XmlNode;
                XmlNode parentNode = xmlNode.ParentNode;

                XmlNode next = xmlNode.NextSibling;

                // move result to a place where only one element exist (no searching)
                tmp.AppendChild(xmlNode);
                // call child operations
                foreach (PatchOperation loopOperation in operations)
                {
                    if (!loopOperation.Apply(xml) && stopOnFail)
                    {
                        result = false;
                        break;
                    }
                }
                // restore element layout
                if (next == null)
                {
                    parentNode.AppendChild(xmlNode); // append to the end
                }
                else
                {
                    parentNode.InsertBefore(xmlNode, next);
                }
            }
            if (result)
            {
                xml.DocumentElement.RemoveChild(tmp);
            }
            return result;
        }

    }
}
