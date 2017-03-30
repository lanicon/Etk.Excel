﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Etk.BindingTemplates.Context;
using Etk.BindingTemplates.Definitions.Binding;
using ExcelInterop = Microsoft.Office.Interop.Excel; 

namespace Etk.Excel.BindingTemplates.Controls.FormulaResult
{
    class ExcelContextItemFormulaResult : BindingContextItem, IBindingContextItemCanNotify, IExcelControl, ISheetCalculate
    {
        #region properties and attributes
        private IEnumerable<INotifyPropertyChanged> objectsToNotify;
        private readonly ExcelBindingDefinitionFormulaResult excelBindingDefinitionFormulaResult;
        private object currentValue;

        public ExcelInterop.Range Range
        { get; private set; }

        public Action<IBindingContextItem, object> OnPropertyChangedAction
        { get; set; }

        public object OnPropertyChangedActionArgs
        { get; set; }     
        #endregion

        #region .ctors
        public ExcelContextItemFormulaResult(IBindingContextElement parent, IBindingDefinition bindingDefinition)
                                            : base(parent, bindingDefinition)
        {
            excelBindingDefinitionFormulaResult = bindingDefinition as ExcelBindingDefinitionFormulaResult;
            CanNotify = excelBindingDefinitionFormulaResult.CanNotify;
            //NestedContextItem = nestedContextItem;

            if (CanNotify)
            {
                objectsToNotify = bindingDefinition.GetObjectsToNotify(DataSource);
                if (objectsToNotify != null)
                {
                    foreach (INotifyPropertyChanged obj in objectsToNotify)
                        obj.PropertyChanged += OnPropertyChanged;
                }
            }
        }
        #endregion

        public void CreateControl(ExcelInterop.Range range)
        {
            this.Range = range;
        }

        public override void RealDispose()
        {
            OnPropertyChangedAction = null;
            if (objectsToNotify != null)
            {
                foreach (INotifyPropertyChanged obj in objectsToNotify)
                    obj.PropertyChanged -= OnPropertyChanged;
                objectsToNotify = null;
            }
            Range = null;
        }

        public override object ResolveBinding()
        {
            if (excelBindingDefinitionFormulaResult.UseFormulaBindingDefinition != null)
                excelBindingDefinitionFormulaResult.UseFormulaBindingDefinition.UpdateDataSource(this.DataSource, (bool)Range.HasFormula);

            if (Range != null && Range.HasFormula)
                return Range.Formula;
            return excelBindingDefinitionFormulaResult.NestedBindingDefinition.ResolveBinding(this.DataSource);
        }

        public override bool UpdateDataSource(object data, out object retValue)
        {
            if (Range.HasFormula)
                retValue = Range.Value2;
            else
                retValue = excelBindingDefinitionFormulaResult.NestedBindingDefinition.UpdateDataSource(this.DataSource, data);
            return true;
        }

        public void OnPropertyChanged(object source, PropertyChangedEventArgs args)
        {
            if (CanNotify && objectsToNotify != null && OnPropertyChangedAction != null)
            {
                if (BindingDefinition.MustNotify(this.DataSource, source, args))
                    OnPropertyChangedAction(this, OnPropertyChangedActionArgs);
            }
        }

        public void OnSheetCalculate()
        {
            if (Range.HasFormula && ! object.Equals(Range.Value2, currentValue))
            {
                ExcelInterop.WorksheetFunction worksheetFunction = ETKExcel.ExcelApplication.Application.WorksheetFunction; 
                if (worksheetFunction.IsError(Range))
                { 
                    Type type = excelBindingDefinitionFormulaResult.NestedBindingDefinition.BindingType;
                    object nullValue = type.IsValueType ? Activator.CreateInstance(type) : null;
                    excelBindingDefinitionFormulaResult.NestedBindingDefinition.UpdateDataSource(this.DataSource, nullValue);
                }
                else
                    excelBindingDefinitionFormulaResult.NestedBindingDefinition.UpdateDataSource(this.DataSource, Range.Value2);
                currentValue = Range.Value2;

                if (worksheetFunction != null)
                {
                    Marshal.ReleaseComObject(worksheetFunction);
                    worksheetFunction = null;
                }
            }
        } 
    }
}
