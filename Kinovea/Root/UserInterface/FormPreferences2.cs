#region License

/*
Copyright © Joan Charmant 2011.
joan.charmant@gmail.com

This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.
*/

#endregion License

using Kinovea.Root.Languages;
using Kinovea.Root.UserInterface.PreferencePanels;
using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.Root.UserInterface
{
    /// <summary>
    ///     FormPreferences2. A dynamically generated form to display preferences.
    ///     It is a host for preferences pages.
    ///     Preferences pages are UserControl conforming to a IPreferencePanel interface.
    ///     The pages should be of size 432; 236 with white background.
    ///     _initPage can be passed to the constructor to directly load a specific page.
    /// </summary>
    public partial class FormPreferences2 : Form
    {
        #region Members

        private readonly List<UserControl> _mPrefPages = new List<UserControl>();
        private readonly List<PreferencePanelButtton> _mPrefsButtons = new List<PreferencePanelButtton>();
        private static readonly int MDefaultPage = 0;

        #endregion Members

        #region Construction and Initialization

        public FormPreferences2(int initPage)
        {
            InitializeComponent();

            if (RootLang.dlgPreferences_Title != null) Text = @"   " + RootLang.dlgPreferences_Title;
            btnSave.Text = RootLang.Generic_Save;
            btnCancel.Text = RootLang.Generic_Cancel;

            ImportPages();
            DisplayPage(initPage);
        }

        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        private void ImportPages()
        {
            // All pages are added dynamically, from a static list.

            //-----------------------------------------------------------------------------------------------------------------
            // Note on architecture:
            // Apparently SharpDevelop designer has trouble loading classes that are not directly deriving from UserControl.
            // Ideally we would have had an "AbstractPreferencePanel" as a generic class and used this everywhere.
            // Unfortunately, in this case, #Develop wouldn't let us graphically design the individual panels.
            // To work around this and still retain the designer, we use UserControl as the base class.
            // Each panel should implement the IPreferencePanel interface to conform to the architecture.
            // (If you wonder, directly creating a List<> from an interface is not allowed in .NET)
            //
            // To create a new Preference page: Add a new file from UserControl template, add IPreferencePanel as an interface.
            // Implement the functions and finally add it to the list here.
            //-----------------------------------------------------------------------------------------------------------------

            _mPrefPages.Add(new PreferencePanelGeneral());
            _mPrefPages.Add(new PreferencePanelPlayer());
            _mPrefPages.Add(new PreferencePanelDrawings());
            _mPrefPages.Add(new PreferencePanelCapture());

            AddPages();
        }

        private void AddPages()
        {
            pnlButtons.Controls.Clear();

            var nextLeft = 0;
            foreach (var t in _mPrefPages)
            {
                var page = t as IPreferencePanel;
                if (page != null)
                {
                    // Button
                    var ppb = new PreferencePanelButtton(page);
                    ppb.Click += preferencePanelButton_Click;

                    ppb.Left = nextLeft;
                    nextLeft += ppb.Width;

                    pnlButtons.Controls.Add(ppb);
                    _mPrefsButtons.Add(ppb);

                    // Page
                    page.Location = new Point(14, pnlButtons.Bottom + 14);
                    page.Visible = false;
                    Controls.Add((UserControl)page);
                }
            }
        }

        #endregion Construction and Initialization

        #region Save & Cancel Handlers

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Ask each page to commit its changes to the PreferencesManager.
            foreach (var userControl in _mPrefPages)
            {
                var page = (IPreferencePanel)userControl;
                page.CommitChanges();
            }

            PreferencesManager.Instance().Export();
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion Save & Cancel Handlers

        #region Private Methods

        private void preferencePanelButton_Click(object sender, EventArgs e)
        {
            // A preference page button has been clicked.
            // Activate the button and load the page.
            var selectedButton = sender as PreferencePanelButtton;
            if (selectedButton != null)
            {
                foreach (var pageButton in _mPrefsButtons)
                {
                    pageButton.SetSelected(pageButton == selectedButton);
                }

                LoadPage(selectedButton);
            }
        }

        private void DisplayPage(int page)
        {
            // This function can be used to directly load the pref dialog on a specific page.
            var pageToDisplay = MDefaultPage;

            if (page > 0 && page < _mPrefPages.Count)
            {
                pageToDisplay = page;
            }

            for (var i = 0; i < _mPrefPages.Count; i++)
            {
                var selected = (i == pageToDisplay);
                _mPrefsButtons[i].SetSelected(selected);
                _mPrefPages[i].Visible = selected;
            }
        }

        private void LoadPage(PreferencePanelButtton button)
        {
            foreach (var userControl in _mPrefPages)
            {
                var prefPanel = (IPreferencePanel)userControl;
                prefPanel.Visible = (prefPanel == button.PreferencePanel);
            }
        }

        #endregion Private Methods
    }
}