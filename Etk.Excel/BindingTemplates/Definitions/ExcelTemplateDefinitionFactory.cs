﻿using System;
using System.Runtime.InteropServices;
using Etk.BindingTemplates.Definitions.Templates;
using Etk.Excel.BindingTemplates.Definitions.Xml;
using Etk.Tools.Extensions;
using Microsoft.Office.Interop.Excel;

namespace Etk.Excel.BindingTemplates.Definitions
{
    class ExcelTemplateDefinitionFactory
    {
        #region const
        //public const string CONTEXTUALMENU_START_FORMAT = "<ContextualMenu*Name='{0}'";
        public const string TEMPLATE_END_FORMAT = "<EndTemplate*Name='{0}'";

        public const string TEMPLATE_START_HEADER_ONELINE = "<Header*/>";
        public const string TEMPLATE_START_HEADER = "<Header*>";
        public const string TEMPLATE_END_HEADER = "</Header*>";

        public const string TEMPLATE_START_FOOTER_ONELINE = "<Footer*/>";
        public const string TEMPLATE_START_FOOTER = "<Footer*>";
        public const string TEMPLATE_END_FOOTER = "</*Footer>";
        #endregion

        #region .ctors
        private ExcelTemplateDefinitionFactory()
        {}
        #endregion

        #region public method
        public static ExcelTemplateDefinition CreateInstance(string templateName, Range templateDeclarationFirstCell)
        {
            ExcelTemplateDefinitionFactory factory = new ExcelTemplateDefinitionFactory();
            return factory.Execute(templateName, templateDeclarationFirstCell);
        }
        #endregion

        #region private method
        private ExcelTemplateDefinition Execute(string templateName, Range templateDeclarationFirstCell)
        {
            Worksheet worksheet = null;
            try
            {
                if (string.IsNullOrEmpty(templateName))
                    throw new EtkException("Template name cannot be null or empty.");

                if (templateDeclarationFirstCell == null)
                    throw new EtkException("Template caller cannot be null.");

                //Get template option
                XmlExcelTemplateOption xmlTemplateOption = (templateDeclarationFirstCell.Value2 as string).Deserialize<XmlExcelTemplateOption>();
                TemplateOption templateOption = new TemplateOption(xmlTemplateOption);

                // Get the template end.
                worksheet = templateDeclarationFirstCell.Worksheet;
                Range templateDeclarationLastRange = worksheet.Cells.Find(string.Format(TEMPLATE_END_FORMAT, templateName), Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                if (templateDeclarationLastRange == null)
                    throw new EtkException(string.Format("Cannot find the end of template '{0}' in sheet '{1}'", templateName.EmptyIfNull(), worksheet.Name.EmptyIfNull()));

                ExcelTemplateDefinition excelTemplateDefinition = new ExcelTemplateDefinition(templateDeclarationFirstCell, templateDeclarationLastRange, templateOption);
                ExcelTemplateDefinitionPart header, body, footer;
                ParseTemplate(excelTemplateDefinition, ref worksheet, out header, out body, out footer);
                excelTemplateDefinition.ExcelInit(header, body, footer);

                return excelTemplateDefinition;
            }
            catch (Exception ex)
            {
                throw new EtkException(string.Format("Cannot create the template '{0}'. {1}", templateName.EmptyIfNull(), ex.Message));
            }
            finally
            {
                if (worksheet != null)
                    Marshal.ReleaseComObject(worksheet);
            }
        }

        /// <summary> Parse the template. Retrieve its components. </summary>
        private void ParseTemplate(ExcelTemplateDefinition excelTemplateDefinition, ref Worksheet worksheet, out ExcelTemplateDefinitionPart header, out ExcelTemplateDefinitionPart body, out ExcelTemplateDefinitionPart footer)
        {
            try
            {
                header = body = footer = null;
                
                int headerSize;
                int footerSize;
                RetrieveHeaderAndFooterSize(excelTemplateDefinition.DefinitionFirstCell, excelTemplateDefinition.DefinitionLastCell, excelTemplateDefinition.Orientation, out headerSize, out footerSize);

                Range firstRange = worksheet.Cells[excelTemplateDefinition.DefinitionFirstCell.Row + 1, excelTemplateDefinition.DefinitionFirstCell.Column + 1];
                Range lastRange = worksheet.Cells[excelTemplateDefinition.DefinitionLastCell.Row, excelTemplateDefinition.DefinitionLastCell.Column - 1];

                int width = lastRange.Column - firstRange.Column + 1;
                int height = lastRange.Row - firstRange.Row + 1;

                // Header
                /////////
                if (headerSize != 0)
                {
                    Range headerLastRange;
                    if (excelTemplateDefinition.Orientation == Etk.BindingTemplates.Definitions.Templates.Orientation.Horizontal)
                        headerLastRange = worksheet.Cells[firstRange.Row + height - 1, firstRange.Column + headerSize - 1];
                    else
                        headerLastRange = worksheet.Cells[firstRange.Row + headerSize - 1, lastRange.Column];
                    //string name = string.Format("{0}-{1}", excelTemplateDefinition.Name, "Header");
                    header = ExcelTemplateDefinitionPartFactory.CreateInstance(excelTemplateDefinition, firstRange, headerLastRange);
                    headerLastRange = null;
                }

                // Footer
                /////////
                if (footerSize != 0)
                {
                    Range footerFirstRange;
                    if (excelTemplateDefinition.Orientation == Etk.BindingTemplates.Definitions.Templates.Orientation.Horizontal)
                        footerFirstRange = worksheet.Cells[lastRange.Row - height + 1, lastRange.Column - footerSize + 1];
                    else
                        footerFirstRange = worksheet.Cells[lastRange.Row - footerSize + 1, firstRange.Column];
                    //string name = string.Format("{0}-{1}", excelTemplateDefinition.Name, "Footer");
                    footer = ExcelTemplateDefinitionPartFactory.CreateInstance(excelTemplateDefinition, footerFirstRange, lastRange);
                    footerFirstRange = null;
                }

                // Body
                ///////
                Range bodyFirstRange;
                Range bodyLastRange;
                if (excelTemplateDefinition.Orientation == Orientation.Horizontal)
                {
                    bodyFirstRange = worksheet.Cells[firstRange.Row, firstRange.Column + headerSize];
                    bodyLastRange = worksheet.Cells[lastRange.Row, lastRange.Column - footerSize];
                }
                else
                {
                    bodyFirstRange = worksheet.Cells[firstRange.Row + headerSize, firstRange.Column];
                    bodyLastRange = worksheet.Cells[lastRange.Row - footerSize, lastRange.Column];
                }

                body = ExcelTemplateDefinitionPartFactory.CreateInstance(excelTemplateDefinition, bodyFirstRange, bodyLastRange);
                bodyFirstRange = bodyLastRange = null;
            }
            catch (Exception ex)
            {
                throw new EtkException(string.Format("The parsing of template '{0}' in sheet '{1}' failed: {2}", excelTemplateDefinition.Name, worksheet.Name.EmptyIfNull(), ex.Message));
            }
        }

        /// <summary>Retrieve the size of the Header and footer</summary>
        private void RetrieveHeaderAndFooterSize(Range firstCell, Range lastCell, Orientation orientation, out int headerSize, out int footerSize)
        {
            headerSize = footerSize = 0;

            Range searchRange = null;
            int width = lastCell.Column - firstCell.Column - 1;
            int height = lastCell.Row - firstCell.Row;

            // Retrieve header and footer
            if (orientation == Orientation.Horizontal)
                searchRange = firstCell.Resize[1, width + 1];
            else
                searchRange = firstCell.Resize[height + 1, 1];

            if (searchRange != null)
            {
                Range endHeader = null;
                Range startHeader = searchRange.Cells.Find(TEMPLATE_START_HEADER_ONELINE, Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                if (startHeader == null)
                {
                    startHeader = searchRange.Cells.Find(TEMPLATE_START_HEADER, Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                    if (startHeader != null)
                    {
                        endHeader = searchRange.Cells.Find(TEMPLATE_END_HEADER, Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                        if (endHeader == null)
                            throw new EtkException("Cannot find the 'Header' end tag");
                    }
                }

                Range endFooter = null;
                Range startFooter = searchRange.Cells.Find(TEMPLATE_START_FOOTER_ONELINE, Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                if (startFooter == null)
                {
                    startFooter = searchRange.Cells.Find(TEMPLATE_START_FOOTER, Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                    if (startFooter != null)
                    {
                        endFooter = searchRange.Cells.Find(TEMPLATE_END_FOOTER, Type.Missing, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false);
                        if (endFooter == null)
                            throw new EtkException("Cannot find the 'Footer' end tag");
                    }
                }

                if (startHeader != null)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        if (startHeader.Column != firstCell.Column + 1)
                            throw new EtkException("The 'Header' tag must be set on the first column after the beginning of the template declaration'");
                        if (endHeader != null && endHeader.Column < startHeader.Column)
                            throw new EtkException("The '<Header>' tag must be set before '<Header/>' one");
                        headerSize = endHeader == null ? 1 : endHeader.Column - startHeader.Column + 1;
                    }
                    else
                    {
                        if (startHeader.Row != firstCell.Row + 1)
                            throw new EtkException("The 'Header' tag must be set on the first dataRow after the beginning of the template declaration'");
                        if (endHeader != null && endHeader.Row < startHeader.Row)
                            throw new EtkException("The '<Header>' tag must be set before '</Header>' one");
                        headerSize = endHeader == null ? 1 : endHeader.Row - startHeader.Row + 1;
                    }
                }

                if (startFooter != null)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        if (endFooter == null && startFooter.Column != firstCell.Column + width ||
                            endFooter != null && endFooter.Column != firstCell.Column + width)
                            throw new EtkException("The end of the 'Footer' tag must be set on last column of the template cells declaration'");
                        if (endFooter != null && startFooter.Column > endFooter.Column)
                            throw new EtkException("The '<Footer>' tag must be set before '</Footer>' one");
                        footerSize = endFooter == null ? 1 : endFooter.Column - startFooter.Column + 1;
                    }
                    else
                    {
                        if (endFooter == null && startFooter.Row != firstCell.Row + height ||
                            endFooter != null && endFooter.Row != firstCell.Row + height)
                            throw new EtkException("The end of the 'Footer' must be set on last dataRow of the template cells declaration'");
                        if (endFooter != null && startFooter.Row > endFooter.Row)
                            throw new EtkException("The '<Footer>' tag must be set before '</Footer>' one");
                        footerSize = endFooter == null ? 1 : endFooter.Row - startFooter.Row + 1;
                    }
                }
                startHeader = startFooter = endHeader = endFooter = null;

            }
            searchRange = null;
        }
        #endregion
    }
}