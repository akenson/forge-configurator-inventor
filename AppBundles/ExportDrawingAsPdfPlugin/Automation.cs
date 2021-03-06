﻿/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
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
using System.Runtime.InteropServices;

using Inventor;
using Autodesk.Forge.DesignAutomation.Inventor.Utils;
using Shared;

namespace ExportDrawingAsPdfPlugin
{
    [ComVisible(true)]
    public class Automation
    {
        private readonly InventorServer inventorApplication;

        public Automation(InventorServer inventorApp)
        {
            inventorApplication = inventorApp;
        }

        public void Run(Document doc)
        {
            


        }

        public void RunWithArguments(Document doc, NameValueMap map)
        {
            using (new HeartBeat())
            {
                var dir = System.IO.Directory.GetCurrentDirectory();
                if (doc == null)
                {
                    ActivateDefaultProject(dir);
                    doc = inventorApplication.Documents.Open(map.Item["_1"]);
                }
                var fullFileName = doc.FullFileName;
                var path = System.IO.Path.GetFullPath(fullFileName);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(fullFileName);
                var drawing = inventorApplication.DesignProjectManager.ResolveFile(path, fileName + ".idw");
                LogTrace("Looking for drawing: " + fileName + ".idw " + "inside: " + path + " with result: " + drawing);
                if (drawing == null)
                {
                    drawing = inventorApplication.DesignProjectManager.ResolveFile(path, fileName + ".dwg");
                    LogTrace("Looking for drawing: " + fileName + ".dwg " + "inside: " + path + " with result: " + drawing);
                }

                if (drawing != null)
                {
                    LogTrace("Found drawing to export at: " + drawing);
                    var drawingDocument = inventorApplication.Documents.Open(drawing);
                    LogTrace("Drawing opened");

                    // Drawing may contain iLogic rules, trigger them if needed
                    LogTrace("Triggering drawing rules");
                    TriggerRule.Trigger(drawingDocument, "iTrigger0");

                    drawingDocument.Update2(true);
                    LogTrace("Drawing updated");
                    drawingDocument.Save2(true);
                    LogTrace("Drawing saved");
                    var pdfPath = System.IO.Path.Combine(dir, "Drawing.pdf");
                    LogTrace("Exporting drawing to: " + pdfPath);
                    ExportIDWToPDF(drawingDocument, pdfPath);
                    //drawingDocument.SaveAs(pdfPath, true);
                    LogTrace("Drawing exported");
                }
            }
        }

        private void ActivateDefaultProject(string dir)
        {
            var defaultProjectName = "FDADefault";
            
            var projectFullFileName = System.IO.Path.Combine(dir, defaultProjectName + ".ipj");

            DesignProject project = null;
            if (System.IO.File.Exists(projectFullFileName))
            {
                project = inventorApplication.DesignProjectManager.DesignProjects.AddExisting(projectFullFileName);
                Trace.TraceInformation("Adding existing default project file: {0}", projectFullFileName);

            } else
            {
                project = inventorApplication.DesignProjectManager.DesignProjects.Add(MultiUserModeEnum.kSingleUserMode, defaultProjectName, dir);
                Trace.TraceInformation("Creating default project file with name: {0} at {1}", defaultProjectName, dir);
            }
            
            Trace.TraceInformation("Activating default project {0}", project.FullFileName);
            project.Activate(true);
        }

        //private void TriggerDrawingRules(Document doc)
        //{
        //    Parameters parameters = ((DrawingDocument)doc).Parameters;

        //    try
        //    {
        //        dynamic trigger = parameters["iTrigger0"];

        //        // Just inccrement the trigger value, right now only supporting numeric triggers
        //        trigger.Value++;
        //        LogTrace("Fired trigger for drawing rules");
        //    }
        //    catch (Exception)
        //    {
        //        LogTrace("No drawing rules to trigger");
        //    }

        // }

        // Export Drawing file to PDF format
        // In case that the Drawing has more sheets -> it will export PDF with pages
        // Each PDF page represet one Drawing sheet
        public void ExportIDWToPDF(Document doc, string exportFileName)
        {
            if (doc == null)
            {
                LogError("Document is null!");
                return;
            }

            LogTrace("PDF file full path : " + exportFileName);

            LogTrace("Create PDF Translator Addin");
            TranslatorAddIn PDFAddIn = (TranslatorAddIn)inventorApplication.ApplicationAddIns.ItemById["{0AC6FD96-2F4D-42CE-8BE0-8AEA580399E4}"];

            if (PDFAddIn == null)
            {
                LogError("Error: PDF Translator Addin is null!");
                return;
            }

            TranslationContext context = inventorApplication.TransientObjects.CreateTranslationContext();
            NameValueMap options = inventorApplication.TransientObjects.CreateNameValueMap();
            if (PDFAddIn.HasSaveCopyAsOptions[doc, context, options])
            {
                context.Type = IOMechanismEnum.kFileBrowseIOMechanism;
                DataMedium dataMedium = inventorApplication.TransientObjects.CreateDataMedium();

                options.Value["Sheet_Range"] = PrintRangeEnum.kPrintAllSheets;
                options.Value["Vector_Resolution"] = 300;

                options.Value["All_Color_AS_Black"] = false;
                options.Value["Sheets"] = GetSheetOptions(doc);

                dataMedium.FileName = exportFileName;
                LogTrace("Processing PDF export ...");
                PDFAddIn.SaveCopyAs(doc, context, options, dataMedium);
                LogTrace("Finish processing PDF export ...");
            }
        }

        // Check if the Drawing file has more sheets
        private NameValueMap GetSheetOptions(Document doc)
        {
            DrawingDocument drawingDocument = doc as DrawingDocument;

            NameValueMap sheets = inventorApplication.TransientObjects.CreateNameValueMap();
            foreach (Sheet sheet in drawingDocument.Sheets)
            {
                NameValueMap option = inventorApplication.TransientObjects.CreateNameValueMap();
                option.Add("Name", sheet.Name);
                option.Add("3DModel", false);
                sheets.Add("Sheet" + sheets.Count + 1, option);
            }

            return sheets;
        }

        #region Logging utilities

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string message)
        {
            Trace.TraceError(message);
        }

        #endregion
    }
}