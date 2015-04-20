/*
Copyright � Joan Charmant 2008.
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
using Kinovea.VideoFiles;
using log4net;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class FormFramesImport : Form
    {
        public FormFramesImport(VideoFile videoFile, long iSelStart, long iSelEnd, bool bForceReload)
        {
            InitializeComponent();

            _mVideoFile = videoFile;
            _mISelStart = iSelStart;
            _mISelEnd = iSelEnd;
            _mBForceReload = bForceReload;

            Text = "   " + ScreenManagerLang.FormFramesImport_Title;
            labelInfos.Text = ScreenManagerLang.FormFramesImport_Infos + " 0 / ~?";
            buttonCancel.Text = ScreenManagerLang.Generic_Cancel;

            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Step = 1;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;

            Application.Idle += IdleDetector;
        }

        #region Properties

        public bool Canceled { get; private set; }

        #endregion Properties

        private void formFramesImport_Load(object sender, EventArgs e)
        {
            //-----------------------------------
            // Le Handle existe, on peut y aller.
            //-----------------------------------
            DoImport();
        }

        private void IdleDetector(object sender, EventArgs e)
        {
            _mIsIdle = true;
        }

        public void DoImport()
        {
            //--------------------------------------------------
            // Lancer le worker (d�clenche bgWorker_DoWork)
            //--------------------------------------------------
            bgWorker.RunWorkerAsync();
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //-------------------------------------------------------------
            // /!\ Cette fonction s'execute dans l'espace du WORKER THREAD.
            // Les fonctions appel�es d'ici ne doivent pas toucher l'UI.
            // Les appels ici sont synchrones mais on peut remonter de
            // l'information par bgWorker_ProgressChanged().
            //-------------------------------------------------------------
            _mVideoFile.BgWorker = bgWorker;
            try
            {
                _mVideoFile.ExtractToMemory(_mISelStart, _mISelEnd, _mBForceReload);
            }
            catch (Exception exp)
            {
                Log.Error("Exception thrown : " + exp.GetType() + " in " + exp.Source + exp.TargetSite.Name);
                Log.Error("Message : " + exp.Message);
                var inner = exp.InnerException;
                while (inner != null)
                {
                    Log.Error("Inner exception : " + inner.Message);
                    inner = inner.InnerException;
                }
            }
            e.Result = 0;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //--------------------------------------------------------------------------------
            // Probl�me possible :
            // Le worker thread va vouloir mettre � jour les donn�es tr�s souvent.
            // Comme le traitement est asynchrone,il se peut qu'il poste des ReportProgress()
            // plus vite qu'ils ne soient trait�s ici.
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

                labelInfos.Text = ScreenManagerLang.FormFramesImport_Infos + " " + iValue + " / ~" + iTotal;
            }
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //----------------------------------------------------------------------
            // On arrive ici lorsque la fonction bgWorker_DoWork() ressort.
            // Les donn�es dans e doivent �tre mise en place dans bgWorker_DoWork();
            //----------------------------------------------------------------------

            // Se d�crocher de l'event Idle.
            Application.Idle -= IdleDetector;
            Hide();
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            if (!Canceled)
            {
                Log.Debug("Cancel of extraction to memory asked.");
                if (bgWorker.IsBusy)
                {
                    // This will set bgWorker.CancellationPending to true,
                    // which we check periodically in VideoFile.ExtractToMemory method.
                    // This will also end the bgWorker immediately,
                    // maybe before we check for the cancellation in the other thread.
                    // The VideoFile will be notified after we return to psui.ImportSelectionToMemory.
                    bgWorker.CancelAsync();
                    Canceled = true;
                    buttonCancel.Enabled = false;
                }
            }
        }

        #region Members

        private readonly VideoFile _mVideoFile;
        private readonly long _mISelStart;
        private readonly long _mISelEnd;
        private bool _mIsIdle = true;
        private readonly bool _mBForceReload;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members
    }
}