﻿using System.Collections.Generic;
using Etk.BindingTemplates.Definitions;
using Etk.BindingTemplates.Definitions.EventCallBacks;
using Etk.BindingTemplates.Definitions.Templates;
using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace Etk.Excel.BindingTemplates.Definitions
{
    public class ExcelTemplateDefinitionPart : TemplateDefinitionPart
    {
        #region attributes and properties
        public int Width
        { get;  }

        public int Height
        { get;  }

        public ExcelInterop.Range DefinitionFirstCell
        { get;  }

        public ExcelInterop.Range DefinitionLastCell
        { get;  }

        public ExcelInterop.Range DefinitionCells
        { get; private set; }

        public IDefinitionPart[,] DefinitionParts
        { get; private set; }

        public List<List<int>> PositionLinkedTemplates
        { get; private set; }

        public bool ContainMultiLinesCells
        { get; set; }

        public List<EventCallback> OnAfterRendering
        { get; set; }

        //@@/// <summary> If a expandable header is defined, contains the binding excelTemplateDefinition used to manage the 'Expand' property of the template (needs a header defined on the template.</summary>
        //public IBindingDefinition ExpanderBindingDefinition
        //{ get; set; }
        #endregion

        #region .ctors
        public ExcelTemplateDefinitionPart(ExcelTemplateDefinition parent, TemplateDefinitionPartType partType, ExcelInterop.Range firstRange, ExcelInterop.Range lastRange) : base(partType)
        {
            Parent = parent;
            DefinitionFirstCell = firstRange;
            DefinitionLastCell = lastRange;

            Width = DefinitionLastCell.Column - DefinitionFirstCell.Column + 1;
            Height = DefinitionLastCell.Row - DefinitionFirstCell.Row + 1;
            if (Width == 0 || Height == 0)
                throw new System.Exception("A template part ('Header','Body' or 'Footer' must have a 'Height' and a 'Width' >= 1");

            ExcelInterop.Range templateRange = DefinitionFirstCell;
            DefinitionCells = DefinitionFirstCell = templateRange.Cells[1, 1];

            DefinitionCells = templateRange.Resize[Height, Width];
            DefinitionParts = new IDefinitionPart[Height, Width];

            PositionLinkedTemplates = new List<List<int>>();
        }
        #endregion
    }
}
