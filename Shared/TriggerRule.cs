/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Design Automation team for Inventor
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using Inventor;
using Autodesk.Forge.DesignAutomation.Inventor.Utils;

namespace Shared
{
    public class TriggerRule
    {
        public static void Trigger(Document doc, string triggerName)
        {

            if (doc.DocumentType == DocumentTypeEnum.kDrawingDocumentObject)
            {
                Parameters parameters = ((DrawingDocument)doc).Parameters;

                try
                {
                    dynamic trigger = parameters[triggerName];

                    // Just inccrement the trigger value, right now only supporting numeric triggers
                    trigger.Value++;
                    LogTrace("Fired trigger " + triggerName);
                }
                catch (Exception)
                {
                    LogTrace("No trigger to fire in document");
                }
            }
            else
            {
                LogTrace("Only drawing triggers are currently supported");
            }

        }

        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }
    }

 
}
