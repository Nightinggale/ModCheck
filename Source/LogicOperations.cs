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
    }

    public class IfElse : ModCheckNameClass
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
    }

    public class Once : ModCheckNameClass
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

    public class Sequence : ModCheckNameClass
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
    }
}
