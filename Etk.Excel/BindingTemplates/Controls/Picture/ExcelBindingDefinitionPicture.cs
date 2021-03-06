﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Etk.BindingTemplates.Context;
using Etk.BindingTemplates.Definitions.Binding;
using Etk.Excel.BindingTemplates.Definitions;
using Etk.Tools.Extensions;

namespace Etk.Excel.BindingTemplates.Controls.Picture
{
    class ExcelBindingDefinitionPicture : BindingDefinition
    {
        #region attributes and properties
        public const string CHECKBOX_TEMPLATE_PREFIX = "<Picture";

        public IBindingDefinition ValueBindingDefinition
        { get; private set; }

        public ExcelPictureDefinition Definition
        { get; private set; }

        public ExcelTemplateDefinition TemplateDefinition
        { get; private set; }
        #endregion

        #region .ctors and factories
        private ExcelBindingDefinitionPicture(BindingDefinitionDescription definitionDescription, ExcelTemplateDefinition templateDefinition, ExcelPictureDefinition definition)
                                              : base(definitionDescription) 
        {
            TemplateDefinition = templateDefinition;
            Definition = definition;
            if (!string.IsNullOrEmpty(definition.Value))
            {
                ValueBindingDefinition = BindingDefinitionFactory.CreateInstances(templateDefinition, DefinitionDescription);
                if (! ValueBindingDefinition.BindingType.Equals(typeof(bool)))
                    throw new EtkException("A 'CheckBox' must be bound with RetrieveContextualMethodInfo boolean value");

                CanNotify = ValueBindingDefinition.CanNotify;
            }
        }

        public static ExcelBindingDefinitionPicture CreateInstance(ExcelTemplateDefinition templateDefinition, string definition)
        {
            ExcelBindingDefinitionPicture ret = null;
            if (! string.IsNullOrEmpty(definition))
            {
                try
                {
                    //ExcelCheckBoxDefinition excelButtonDefinition = definition.Deserialize<ExcelCheckBoxDefinition>();
                    //BindingDefinitionDescription definitionDescription = BindingDefinitionDescription.CreateBindingDescription(excelButtonDefinition.Value, excelButtonDefinition.Value);
                    //ret = new ExcelBindingDefinitionCheckBox(definitionDescription, templateDefinition, excelButtonDefinition);
                }
                catch (Exception ex)
                {
                    string message = $"Cannot retrieve the checkbox dataAccessor '{definition.EmptyIfNull()}'. {ex.Message}";
                    throw new EtkException(message);
                }
            }
            return ret;
        }
        #endregion

        #region public methods
        public override object UpdateDataSource(object dataSource, object data)
        {
            return ValueBindingDefinition == null ? null : ValueBindingDefinition.UpdateDataSource(dataSource, data);
        }

        public override object ResolveBinding(object dataSource)
        {
            return ValueBindingDefinition == null ? null : ValueBindingDefinition.ResolveBinding(dataSource);
        }

        public override IBindingContextItem ContextItemFactory(IBindingContextElement parent)
        {
            return new ExcelContextItemPicture(parent, this);
        }

        public override IEnumerable<INotifyPropertyChanged> GetObjectsToNotify(object dataSource)
        {
            return ValueBindingDefinition == null ? null : ValueBindingDefinition.GetObjectsToNotify(dataSource);
        }

        public override bool MustNotify(object dataSource, object source, PropertyChangedEventArgs args)
        {
            return ValueBindingDefinition == null ? false : ValueBindingDefinition.MustNotify(dataSource, source, args);
        }
        #endregion
    }
}
