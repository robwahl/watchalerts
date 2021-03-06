/*
Copyright © Joan Charmant 2008.
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

using Kinovea.Root.Languages;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using Kinovea.Services;

namespace Kinovea.Root
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
                        	
            this.Text = "   " + RootLang.mnuAbout;

            int iSmallFontSize = 7;
            
            
            rtbInfos.Clear();
            rtbInfos.AppendText("\n ");
            rtbInfos.AppendText(RootLang.dlgAbout_Info1); // GNU GPL
            rtbInfos.AppendText("\n\n ");
            rtbInfos.AppendText(RootLang.dlgAbout_Info3); // Help welcome.
            rtbInfos.AppendText("\n\n ");
            
            rtbInfos.SelectionFont = new Font("Microsoft Sans Serif", iSmallFontSize, FontStyle.Bold | FontStyle.Underline); 
            rtbInfos.AppendText(RootLang.dlgAbout_Info4); // Localisations.
			rtbInfos.SelectionFont = new Font("Microsoft Sans Serif", iSmallFontSize, FontStyle.Regular); 
			rtbInfos.AppendText("\n\n ");
			
			rtbInfos.AppendText(" " + LanguageManager.Dutch);
			AddName("Peter Strikwerda, Bart Kerkvliet");
            rtbInfos.AppendText(" " + LanguageManager.German);
            AddName("Stephan Frost, Dominique Saussereau, Jonathan Boder, Stephan Peuckert");
            rtbInfos.AppendText(" " + LanguageManager.Portuguese);
            AddName("Fernando Jorge, Rafael Fernandes");
            rtbInfos.AppendText(" " + LanguageManager.Spanish);
            AddName("Rafael Gonzalez, Lionel Sosa Estrada, Andoni Morales Alastruey");
            rtbInfos.AppendText(" " + LanguageManager.Italian);
            AddName("Giorgio Biancuzzi");
            rtbInfos.AppendText(" " + LanguageManager.Romanian);
            AddName("Bogdan Paul Frăţilă");
			rtbInfos.AppendText(" " + LanguageManager.Polish);
            AddName("Kuba Zamojski");
			rtbInfos.AppendText(" " + LanguageManager.Finnish);
            AddName("Alexander Holthoer");
			rtbInfos.AppendText(" " + LanguageManager.Norwegian);
            AddName("Espen Kolderup");		
            
            // Special case for Chinese since 7 is not enough.
            rtbInfos.SelectionFont = new Font("Microsoft Sans Serif", 9, FontStyle.Regular); 
            rtbInfos.AppendText(" " + LanguageManager.Chinese);
            rtbInfos.SelectionColor = Color.SeaGreen;
    		rtbInfos.AppendText(String.Format(" - Nicko Deng.\n"));
        	rtbInfos.SelectionColor = Color.Black;
            rtbInfos.SelectionFont = new Font("Microsoft Sans Serif", iSmallFontSize, FontStyle.Regular); 
            
			rtbInfos.AppendText(" " + LanguageManager.Turkish);
			AddName("Eray Kıranoğlu");
			rtbInfos.AppendText(" " + LanguageManager.Greek);
			AddName("Nikos Sklavounos");
            
			rtbInfos.AppendText(" " + LanguageManager.Lithuanian);
			AddName("Mindaugas Slavikas");
			
			rtbInfos.AppendText(" " + LanguageManager.Swedish);
			AddName("Thomas Buska, Alexander Holthoer");
			
			rtbInfos.AppendText("\n ");
			rtbInfos.SelectionFont = new Font("Microsoft Sans Serif", iSmallFontSize, FontStyle.Bold | FontStyle.Underline); 
            rtbInfos.AppendText(RootLang.dlgAbout_Info2); // External libs.
            rtbInfos.SelectionFont = new Font("Microsoft Sans Serif", iSmallFontSize, FontStyle.Regular); 
			rtbInfos.AppendText("\n\n ");
			
			rtbInfos.AppendText(" FFmpeg - Video formats and codecs - The FFmpeg contributors.\n");
            rtbInfos.AppendText(" AForge - Image processing - Andrew Kirillov and contributors.\n");
            rtbInfos.AppendText(" ExpTree - Explorer Treeview - Jim Parsells.\n");
            rtbInfos.AppendText(" FileDownloader - Phil Crosby.\n");
            rtbInfos.AppendText(" log4Net - Logging utility - Apache Foundation.\n");
            rtbInfos.AppendText(" #ZipLib - Compression utility - Mike Krueger and contributors.\n");
       		rtbInfos.AppendText(" OpenCV - Computer Vision - The OpenCV contributors.\n");
			rtbInfos.AppendText(" EmguCV - OpenCV Wrapper - Canming.\n");
			rtbInfos.AppendText(" Sharp Vector Graphics - SVG import and rendering - SVG# contributors.\n");
       		
            labelCopyright.Text = @"Copyright © 2006-2011 - Joan Charmant && Contrib.";
            lblKinovea.Text = "Kinovea - " + PreferencesManager.ReleaseVersion;

            // website.
            lnkKinovea.Links.Clear();
            lnkKinovea.Links.Add(0, lnkKinovea.Text.Length, "http://www.kinovea.org");
        }
        
        private void AddName(String _name)
        {
        	rtbInfos.SelectionColor = Color.SeaGreen;
        	rtbInfos.AppendText(String.Format(" - {0}.\n", _name));
            rtbInfos.SelectionColor = Color.Black;
        }

        private void lnkKinovea_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Launch default web browser
            ProcessStartInfo sInfo = new ProcessStartInfo(e.Link.LinkData.ToString());
            Process.Start(sInfo);
        }
    }
}