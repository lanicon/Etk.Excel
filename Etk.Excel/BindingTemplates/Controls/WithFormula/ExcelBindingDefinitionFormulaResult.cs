﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Etk.BindingTemplates.Context;
using Etk.BindingTemplates.Definitions.Binding;
using Etk.Excel.BindingTemplates.Definitions;
using Etk.Tools.Extensions;

namespace Etk.Excel.BindingTemplates.Controls.WithFormula
{
    class ExcelBindingDefinitionFormulaResult : BindingDefinition
    {
        #region attributes and properties
        public const string FORMULA_RESULT_PREFIX = "{>";

        public IBindingDefinition UseFormulaBindingDefinition
        { get; private set; }

        public IBindingDefinition NestedBindingDefinition
        { get; private set; }

        public override string Name => NestedBindingDefinition != null ? NestedBindingDefinition.Name : string.Empty;

        public override string Description => NestedBindingDefinition != null ? NestedBindingDefinition.Description : string.Empty;

        #endregion

        #region .ctors and factories
        private ExcelBindingDefinitionFormulaResult(BindingDefinitionDescription bindingDefinitionDescription, IBindingDefinition underlyingBindingDefinition, IBindingDefinition useFormulaBindingDefinition)
                                                    : base(bindingDefinitionDescription) 
        {
            NestedBindingDefinition = underlyingBindingDefinition;
            UseFormulaBindingDefinition = useFormulaBindingDefinition;
            CanNotify = underlyingBindingDefinition.CanNotify;

            DefinitionDescription = new BindingDefinitionDescription();
        }

        public static ExcelBindingDefinitionFormulaResult CreateInstance(ExcelTemplateDefinition templateDefinition, string definition)
        {
            try
            {
                definition = definition.Replace(FORMULA_RESULT_PREFIX, string.Empty);
                definition = definition.TrimEnd('}');

                //UseFormulaBindingDefinition
                string[] parts = definition.Split(';');
                if (parts.Count() > 2)
                    throw new ArgumentException($"dataAccessor '{definition}' is invalid.");

                string useFormulaDefinition = null;
                string underlyingDefinition;
                if (parts.Count() == 1)
                    underlyingDefinition = $"{{{parts[0].Trim()}}}";
                else
                {
                    useFormulaDefinition = $"{{{parts[0].Trim()}}}";
                    underlyingDefinition = $"{{{parts[1].Trim()}}}";
                }

                BindingDefinitionDescription bindingDefinitionDescription = BindingDefinitionDescription.CreateBindingDescription(templateDefinition, underlyingDefinition, underlyingDefinition);
                IBindingDefinition underlyingBindingDefinition = BindingDefinitionFactory.CreateInstances(templateDefinition, bindingDefinitionDescription);

                IBindingDefinition useFormulaBindingDefinition = null;
                if (!string.IsNullOrEmpty(useFormulaDefinition))
                {
                    bindingDefinitionDescription = BindingDefinitionDescription.CreateBindingDescription(templateDefinition, useFormulaDefinition, useFormulaDefinition);
                    useFormulaBindingDefinition = BindingDefinitionFactory.CreateInstances(templateDefinition, bindingDefinitionDescription);
                }
                ExcelBindingDefinitionFormulaResult ret = new ExcelBindingDefinitionFormulaResult(bindingDefinitionDescription, underlyingBindingDefinition, useFormulaBindingDefinition);
                return ret;
            }
            catch (Exception ex)
            {
                string message = $"Cannot retrieve the formula result binding dataAccessor '{definition.EmptyIfNull()}'. {ex.Message}";
                throw new EtkException(message);
            }
        }
        #endregion

        public override IBindingContextItem ContextItemFactory(IBindingContextElement parent)
        {
            //IBindingContextItem nestedContextItem = NestedBindingDefinition.ContextItemFactory(parent);
            return new ExcelContextItemFormulaResult(parent, this);
        }

        //  cannot be reached... Managed in 'ExcelContextItemFormulaResult'
        public override object UpdateDataSource(object dataSource, object data)
        {
            return null;
        }

        //  cannot be reached... Managed in 'ExcelContextItemFormulaResult'
        public override object ResolveBinding(object dataSource)
        {
            return null;
        }

        public override bool MustNotify(object dataSource, object source, PropertyChangedEventArgs args)
        {
            return NestedBindingDefinition != null && NestedBindingDefinition.MustNotify(dataSource, source, args);
        }

        public override IEnumerable<INotifyPropertyChanged> GetObjectsToNotify(object dataSource)
        {
            return NestedBindingDefinition?.GetObjectsToNotify(dataSource);
        }
    }
}
