using System.Xml;
using Verse;
using System;

namespace ModCheck
{
#pragma warning disable CS0649

    public abstract class ModCheckLog : ModCheckNameClass
    {
        protected string MessageSuccess;
        protected string MessageFail;
        protected string WarningSuccess;
        protected string WarningFail;
        protected string ErrorSuccess;
        protected string ErrorFail;

        protected string VerboseMessageSuccess;
        protected string VerboseMessageFail;
        protected string VerboseWarningSuccess;
        protected string VerboseWarningFail;
        protected string VerboseErrorSuccess;
        protected string VerboseErrorFail;
  
        protected bool errorOnFail = false;

        // legacy strings
        protected string customMessageSuccess;
        protected string customMessageFail;

        public enum Seriousness { Message, Warning, Error };

        protected virtual string getDefaultErrorString()
        {
            Log.Error("Base getDefaultErrorString() called");
            return null;
        }

        private string makeString(string input)
        {
            string arg0 = Memory.getCurrentModName();    // mod, which is being patched
            string arg1 = Memory.getCurrentFileName();   // name of xml file being patched
            string arg2 = Memory.getCurrentPatchOwner(); // name of the mod providing the patch file
            string arg3 = Memory.getCurrentPatchName();  // name of the root operation of the current patch operation (empty string if none is set)
            string arg4 = this.getPatchName();           // name of the current operation


            // NOTE: when adding new arguments, update the number in the error message
            try
            {
                return String.Format(input, arg0, arg1, arg2, arg3, arg4);
            }
            catch
            {
                Log.Error("[ModCheck] Mod " + arg2 + " " + arg3 + " - " + arg4 + " is using out of range argument IDs in the following string: (max number is 4)\n" + input);
                return null;
            }
        }

        protected void printString(string input, Seriousness type)
        {
            if (input.NullOrEmpty())
            {
                return;
            }
            string output = makeString(input);
            if (output.NullOrEmpty())
            {
                return;
            }
            switch(type)
            {
                case Seriousness.Message: { Log.Message(output); break; }
                case Seriousness.Warning: { Log.Warning(output); break; }
                case Seriousness.Error  : { Log.Error  (output); break; }
            }
        }


        protected void printLogMessages(bool success)
        {
            if (success)
            {
                printString(MessageSuccess, Seriousness.Message);
                printString(WarningSuccess, Seriousness.Warning);
                printString(ErrorSuccess, Seriousness.Error);
                if (Prefs.LogVerbose)
                {
                    printString(VerboseMessageSuccess, Seriousness.Message);
                    printString(VerboseWarningSuccess, Seriousness.Warning);
                    printString(VerboseErrorSuccess, Seriousness.Error);
                }
            }
            else
            {
                printString(MessageFail, Seriousness.Message);
                printString(WarningFail, Seriousness.Warning);
                printString(ErrorFail, Seriousness.Error);
                if (Prefs.LogVerbose)
                {
                    printString(VerboseMessageFail, Seriousness.Message);
                    printString(VerboseWarningFail, Seriousness.Warning);
                    printString(VerboseErrorFail, Seriousness.Error);
                }
            }

            // legacy code
            string messageStr = null;

            if (success)
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
                if (!success && errorOnFail)
                {
                    printString(messageStr, Seriousness.Error);
                }
                else
                {
                    printString(messageStr, Seriousness.Message);
                }
            }


        }

    }

    public class LogWrite : logWrite { }
    public class logWrite : ModCheckLog
    {
        bool Once = true;
        bool internalExecuted = false;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (internalExecuted && Once)
            {
                return true;
            }
            printLogMessages(true);

            internalExecuted = true;
            return true;
        }
    }
}
