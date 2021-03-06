﻿using Etk.BindingTemplates.Context;
using Etk.BindingTemplates.Context.SortSearchAndFilter;
using Etk.BindingTemplates.Definitions.SortSearchAndFilter;
using Etk.BindingTemplates.Views;
using Etk.Excel.BindingTemplates.Views;
using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace Etk.Excel.BindingTemplates.SortSearchAndFilter
{
    class ExcelBindingSearchContextItem : BindingSearchContextItem
    {
        public ExcelInterop.Range DestinationRange
        { get; private set; }

        public ExcelBindingSearchContextItem(ITemplateView view, BindingSearchDefinition definition, IBindingContextElement parent)
                                            : base(view, definition, parent)
        {
             ((ExcelTemplateView) view).RegisterSearchControl(this);   
        }

        public void SetRange(ref ExcelInterop.Range range)
        {
            DestinationRange = range;    
        }
    }
}
