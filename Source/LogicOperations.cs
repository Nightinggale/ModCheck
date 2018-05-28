using System.Xml;
using Verse;
using System.Collections.Generic;

namespace ModCheck
{
#pragma warning disable CS0649

    public class AND : PatchOperation
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

    public class OR : PatchOperation
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

    public class IfElse : PatchOperation
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

    public class Once : PatchOperation
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
}
