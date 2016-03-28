﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Etk.Tools.Log;
using Microsoft.Office.Interop.Excel;

namespace Etk.Excel.Application
{
    class ExcelNotifyPropertyManager : IDisposable
    {
        private volatile bool waitExcelBusy;
        private bool isDisposed;
        private readonly object syncObj = new object();
        private readonly BlockingCollection<ExcelNotityPropertyContext> contextItems;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly ExcelApplication ExcelApplication;
        
        private readonly Thread thread;

        #region .ctors
        public ExcelNotifyPropertyManager(ExcelApplication excelApplication)
        {
            contextItems = new BlockingCollection<ExcelNotityPropertyContext>();
            ExcelApplication = excelApplication;

            thread = new Thread(Execute);
            thread.Name = "NotifyPropertiesChanged";
            thread.IsBackground = true;
            thread.Start();
        }
        #endregion
        
        #region public methods
        public void NotifyPropertyChanged(ExcelNotityPropertyContext context)
        {
            if (isDisposed)
                return;

            if (contextItems.FirstOrDefault(i => i.ContextItem == context.ContextItem && ! i.ChangeColor) != null)
                return;
            else
                contextItems.Add(context);
        }

        public void Dispose()
        {
            try
            {
                lock (syncObj)
                {
                    if (!isDisposed)
                    {
                        isDisposed = true;
                        cancellationTokenSource.Cancel();
                    }
                }
            }
            catch
            {}
        }
        #endregion

        #region private methods
        //private void NotifyChangeColor(ExcelNotityPropertyContext bindingContextPart)
        //{
        //    if (isDisposed)
        //        return;

        //    if (contextItems.FirstOrDefault(i => i.ContextItem == bindingContextPart.ContextItem && bindingContextPart.ChangeColor) != null)
        //        return;
        //    else
        //        contextItems.Add(bindingContextPart);
        //}

        private void Execute()
        {
            try
            {
                //long gap = 0;
                while (!isDisposed)
                {
                    if (waitExcelBusy)
                    {
                        Thread.Sleep(50);
                        waitExcelBusy = false;
                        try
                        {
                            ExcelApplication.Application.EnableEvents = true;
                        }
                        catch { }
                    }

                    ExcelNotityPropertyContext context = contextItems.Take(cancellationTokenSource.Token);
                    if (context != null)
                        (ETKExcel.ExcelApplication as ExcelApplication).ExcelDispatcher.BeginInvoke(new System.Action(() => ExecuteNotity(context)));
                    else
                        Logger.Instance.Log(LogType.Info, "PostAsynchronousManager properly ended");
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    Logger.Instance.Log(LogType.Info, "ExcelNotifyPropertyManager properly ended");
                else
                    Logger.Instance.LogException(LogType.Error, ex, string.Format("ExcelNotifyPropertyManager not properly ended", ex.Message));
            }
            finally
            {
                contextItems.Dispose();
            }
        }

        private void ExecuteNotity(ExcelNotityPropertyContext context)
        {
            if (isDisposed)
                return;

            if (context.ContextItem.IsDisposed || ! context.View.IsRendered)
                return;

            Worksheet worksheet = context.View.FirstOutputCell.Worksheet;
            Worksheet activeWorksheet = ExcelApplication.GetActiveSheet();
            Range range = null;
            bool enableEvent = ExcelApplication.Application.EnableEvents;
            bool enableEventChanged = false;
            try
            {
                KeyValuePair<int, int> kvp = context.Param;
                range = worksheet.Cells[context.View.FirstOutputCell.Row + kvp.Key, context.View.FirstOutputCell.Column + kvp.Value];
                if (range != null)
                {
                    //if (bindingContextPart.ChangeColor)
                    //{
                    //    ExcelApplication.Application.EnableEvents = false;
                    //    concernedRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.OldLace);
                    //}
                    //else
                    {
                        object value = context.ContextItem.ResolveBinding();
                        if (!object.Equals(range.Value2, value))
                        {
                            ExcelApplication.Application.EnableEvents = false;
                            enableEventChanged = true;
                            range.Value2 = value;

                            if (context.ContextItem.BindingDefinition.DecoratorDefinition != null)
                            {
                                Range currentSelectedRange = context.View.CurrentSelectedCell;
                                context.ContextItem.BindingDefinition.DecoratorDefinition.Resolve(range, context.ContextItem);
                                if (currentSelectedRange != null)
                                    currentSelectedRange.Select();
                            }

                            //if (activeWorksheet == worksheet)
                            //{
                            //    object color = concernedRange.Interior.Color;
                            //    concernedRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.OrangeRed);
                            //    Task task = new Task(() =>
                            //    {
                            //        Thread.Sleep(333);
                            //        NotifyChangeColor(new NotityPropertContext(bindingContextPart.ContextItem, bindingContextPart.View, bindingContextPart.Param, true));
                            //    });
                            //    task.Start();
                            //}
                        }
                    }
                }
            }
            catch (COMException)
            {
                waitExcelBusy = true;
                NotifyPropertyChanged(context);
            }
            catch (Exception ex)
            {
                string message = string.Format("'ExecuteNotity' failed.{0}", ex.Message);
                Logger.Instance.LogException(LogType.Error, ex, message);
            }
            finally
            {
                try
                {
                    if (enableEventChanged && enableEvent)
                        ExcelApplication.Application.EnableEvents = enableEvent;
                }
                catch
                { }
            }
            Marshal.ReleaseComObject(worksheet);
            Marshal.ReleaseComObject(activeWorksheet);
            range = null;
            worksheet = null;
            activeWorksheet = null;
        }
        #endregion
    }
}
