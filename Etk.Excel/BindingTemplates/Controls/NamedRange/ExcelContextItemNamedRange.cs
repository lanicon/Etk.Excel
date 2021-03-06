﻿using System.ComponentModel;
using System.Runtime.InteropServices;
using Etk.BindingTemplates.Context;
using Etk.BindingTemplates.Definitions.Binding;
using Etk.Excel.BindingTemplates.Controls.WithFormula;
using ExcelInterop = Microsoft.Office.Interop.Excel;
using Etk.Excel.Application;

namespace Etk.Excel.BindingTemplates.Controls.NamedRange
{
    class ExcelContextItemNamedRange : BindingContextItem, IBindingContextItemCanNotify, IExcelControl, IFormulaCalculation
    {
        #region properties and attributes
        private readonly ExcelBindingDefinitionNamedRange excelBindingDefinitionNamedRange;
        private readonly string name;
        private ExcelInterop.Name rangeName;

        public ExcelInterop.Range Range
        { get; private set; }

        public IBindingContextItem NestedContextItem
        { get; private set; }

        public System.Action<IBindingContextItem, object> OnPropertyChangedAction
        {
            get 
            { 
                if(NestedContextItem == null || ! (NestedContextItem is IBindingContextItemCanNotify))
                    return null;
                return ((IBindingContextItemCanNotify) NestedContextItem).OnPropertyChangedAction;
            }
            set
            {
                if (NestedContextItem != null && NestedContextItem is IBindingContextItemCanNotify)
                    ((IBindingContextItemCanNotify) NestedContextItem).OnPropertyChangedAction = value;
            }
        }

        public object OnPropertyChangedActionArgs
        {
            get 
            { 
                if(NestedContextItem == null || ! (NestedContextItem is IBindingContextItemCanNotify))
                    return null;
                return ((IBindingContextItemCanNotify) NestedContextItem).OnPropertyChangedActionArgs;
            }
            set
            {
                if (NestedContextItem != null && NestedContextItem is IBindingContextItemCanNotify)
                    ((IBindingContextItemCanNotify) NestedContextItem).OnPropertyChangedActionArgs = value;
            } 
        }
        #endregion

        #region .ctors
        public ExcelContextItemNamedRange(IBindingContextElement parent, string name, IBindingDefinition bindingDefinition, IBindingContextItem nestedContextItem)
                                         : base(parent, bindingDefinition)
        {
            this.name = name;
            excelBindingDefinitionNamedRange = bindingDefinition as ExcelBindingDefinitionNamedRange;
            CanNotify = excelBindingDefinitionNamedRange.CanNotify;
            NestedContextItem = nestedContextItem;
        }
        #endregion

        #region public methods
        public void CreateControl(ExcelInterop.Range range)
        {
            ExcelInterop.Worksheet workSheet = null;
            try
            {
                Range = range;
                workSheet = Range.Worksheet;
                if (!string.IsNullOrEmpty(name))
                {
                    ExcelInterop.Names names = null;
                    try
                    {
                        names = workSheet.Names;
                        rangeName = names.Add(name, Range);
                    }
                    catch (COMException ex)
                    {
                        throw new EtkException($"Cannot create named caller '{name}': {ex.Message}");
                    }
                    finally
                    {
                        if (names != null)
                        {
                            ExcelApplication.ReleaseComObject(names);
                            names = null;
                        }
                    }
                }

                if (NestedContextItem != null && NestedContextItem is IExcelControl)
                    ((IExcelControl) NestedContextItem).CreateControl(range);
            }
            finally
            {
                if (workSheet != null)
                {
                    ExcelApplication.ReleaseComObject(workSheet);
                    workSheet = null;
                }
            }
        }

        public override void RealDispose()
        {
            if (rangeName != null)
                rangeName.Delete();

            if (NestedContextItem != null)
                NestedContextItem.Dispose();

            ExcelApplication.ReleaseComObject(rangeName);
            ExcelApplication.ReleaseComObject(Range);

            rangeName = null;
            Range = null;
        }

        public override object ResolveBinding()
        {
            return NestedContextItem == null ? null : NestedContextItem.ResolveBinding();
        }

        public override bool UpdateDataSource(object data, out object retValue)
        {
            if(NestedContextItem != null)
                return NestedContextItem.UpdateDataSource(data, out retValue);
            else
            {
                retValue = null;
                return false;
            }
        }

        public void OnPropertyChanged(object source, PropertyChangedEventArgs args)
        {
            if (NestedContextItem != null && NestedContextItem is IBindingContextItemCanNotify)
                ((IBindingContextItemCanNotify) NestedContextItem).OnPropertyChanged(source, args);
        }

        public void OnSheetCalculate()
        {
            if(NestedContextItem != null && NestedContextItem is IFormulaCalculation)
                ((IFormulaCalculation)NestedContextItem).OnSheetCalculate();
        }
        #endregion
    }
}
