﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Interop;
using Etk.Excel.ContextualMenus;
using Etk.Excel.Extensions;
using Etk.Excel.UI.Windows.ViewsAndtemplates;
using Etk.Excel.UI.Windows.ViewsAndtemplates.ViewModels;

namespace Etk.Excel.TemplateManagement
{
    class TemplateContextualMenuManager : IDisposable
    {
        #region attributes and properties
        private IContextualMenu addTemplateMenu;
        private IContextualMenu manageTemplateMenu;
        private ContextualMenuManager contextualMenuManager;
        #endregion

        #region .ctors
        public TemplateContextualMenuManager(ContextualMenuManager contextualMenuManager)
        {
            this.contextualMenuManager = contextualMenuManager;

            // Create the contextual menu instances. 
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (TextReader textReader = new StreamReader(assembly.GetManifestResourceStream("Etk.Excel.Resources.TemplateManagerAddContextualMenu.xml")))
            {
                string menuXml = textReader.ReadToEnd();
                addTemplateMenu = null;//addTemplateMenu = ContextualMenuFactory.CreateInstances(menuXml).FirstOrDefault();
            }
            using (TextReader textReader = new StreamReader(assembly.GetManifestResourceStream("Etk.Excel.Resources.TemplateManagerUpdateDeleteContextualMenu.xml")))
            {
                string menuXml = textReader.ReadToEnd();
                manageTemplateMenu = null;//ùùmanageTemplateMenu = ContextualMenuFactory.CreateInstances(menuXml).FirstOrDefault();
            }

            // Declare the contextual menus activators. 
            contextualMenuManager.OnContextualMenusRequested += ManageTemplateManagerContextualMenu;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Manage the templates contextual menus
        /// </summary>
        public IEnumerable<IContextualMenu> ManageTemplateManagerContextualMenu(Microsoft.Office.Interop.Excel.Worksheet sheet, Microsoft.Office.Interop.Excel.Range range)
        {
            List<IContextualMenu> menus = new List<IContextualMenu>();
            menus.Add(addTemplateMenu);
            menus.Add(manageTemplateMenu);
            foreach (IContextualMenu menu in menus)
            {
                if(menu != null)
                    (menu as ContextualMenu).SetAction(range);
            }
            return menus;
        }

        public void Dispose()
        {
            contextualMenuManager.OnContextualMenusRequested -= ManageTemplateManagerContextualMenu;
            contextualMenuManager = null;
        }
        #endregion

        #region Contextual menu method handlers   
        /// <summary> Template creation</summary>
        /// <param name="caller">Range where to create the menu</param>
        public static void AddTemplate(Microsoft.Office.Interop.Excel.Range caller)
        {
            using (ExcelMainWindow excelWindow = new ExcelMainWindow(caller.Application.Hwnd))
            {
                Microsoft.Office.Interop.Excel.Range firstOutputRange = caller.Offset[0, 1];

                TemplateManagementViewModel viewModel = new TemplateManagementViewModel(null);
                TemplateManagementWindow window = new TemplateManagementWindow(viewModel);

                WindowInteropHelper windowInteropHelper = new WindowInteropHelper(window);
                windowInteropHelper.Owner = excelWindow.Handle;
                if (window.ShowDialog().GetValueOrDefault())
                { 
                    
                }
            }
        }

        /// <summary> Template modification</summary>
        public static void ModifyTemplate(Microsoft.Office.Interop.Excel.Range caller)
        { 
        }

        /// <summary> Template suppression</summary>
        public static void DeleteTemplate(Microsoft.Office.Interop.Excel.Range caller)
        {
        }
        #endregion
    }
}