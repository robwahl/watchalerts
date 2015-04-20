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

using Kinovea.ScreenManager.Languages;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class FormFramesExport : Form
    {
        private readonly bool _mBBlendDrawings;
        private readonly bool _mBKeyframesOnly;
        private readonly string _mFilePath;
        private readonly int _mIEstimatedTotal;
        private readonly long _mIIntervalTimeStamps;
        private readonly PlayerScreenUserInterface _mPsui;
        private bool _mIsIdle = true;

        public FormFramesExport(PlayerScreenUserInterface psui, string filePath, long iIntervalTimeStamps,
            bool bBlendDrawings, bool bKeyframesOnly, int iEstimatedTotal)
        {
            InitializeComponent();

            _mPsui = psui;
            _mFilePath = filePath;
            _mIIntervalTimeStamps = iIntervalTimeStamps;
            _mBBlendDrawings = bBlendDrawings;
            _mBKeyframesOnly = bKeyframesOnly;
            _mIEstimatedTotal = iEstimatedTotal;

            Text = "   " + ScreenManagerLang.FormFramesExport_Title;
            labelInfos.Text = ScreenManagerLang.FormFramesExport_Infos + " 0 / ~?";

            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Step = 1;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;

            Application.Idle += IdleDetector;
        }

        private void formFramesExport_Load(object sender, EventArgs e)
        {
            //-----------------------------------
            // Le Handle existe, on peut y aller.
            //-----------------------------------
            DoExport();
        }

        private void IdleDetector(object sender, EventArgs e)
        {
            _mIsIdle = true;
        }

        public void DoExport()
        {
            //--------------------------------------------------
            // Lancer le worker (déclenche bgWorker_DoWork)
            //--------------------------------------------------
            bgWorker.RunWorkerAsync();
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //-------------------------------------------------------------
            // /!\ Cette fonction s'execute dans l'espace du WORKER THREAD.
            // Les fonctions appelées d'ici ne doivent pas toucher l'UI.
            // Les appels ici sont synchrones mais on peut remonter de
            // l'information par bgWorker_ProgressChanged().
            //-------------------------------------------------------------
            _mPsui.SaveImageSequence(bgWorker, _mFilePath, _mIIntervalTimeStamps, _mBBlendDrawings, _mBKeyframesOnly,
                _mIEstimatedTotal);

            e.Result = 0;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //--------------------------------------------------------------------------------
            // Cette fonction s'execute dans le thread local.
            // On a le droit de mettre à jour l'UI.
            //--------------------------------------------------------------------------------

            //--------------------------------------------------------------------------------
            // Problème possible :
            // Le worker thread va vouloir mettre à jour les données très souvent.
            // Comme le traitement est asynchrone,il se peut qu'il poste des ReportProgress()
            // plus vite qu'ils ne soient traités ici.
            // Il faut donc attendre que la form soit idle.
            //--------------------------------------------------------------------------------
            if (_mIsIdle)
            {
                _mIsIdle = false;

                var iTotal = (int)e.UserState;
                var iValue = e.ProgressPercentage;

                if (iValue > iTotal)
                {
                    iValue = iTotal;
                }

                progressBar.Maximum = iTotal;
                progressBar.Value = iValue;

                labelInfos.Text = ScreenManagerLang.FormFramesExport_Infos + " " + iValue + " / ~" + iTotal;
            }
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //----------------------------------------------------------------------
            // On arrive ici lorsque la fonction bgWorker_DoWork() ressort.
            // Les données dans e doivent être mise en place dans bgWorker_DoWork();
            //----------------------------------------------------------------------

            // Se décrocher de l'event Idle.
            Application.Idle -= IdleDetector;

            Hide();
        }
    }
}