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
using Kinovea.Services;
using System;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    //--------------------------------------------
    // CommandLoadMovieInScreen.
    //
    // Gestion des écrans.
    // Charge le fichier spécifié dans un écran, en créé un si besoin.
    // Ou demande ce qu'il faut faire en fonction des options de config.
    // Affiche le nouvel écran avec la vidéo dedans, prête.
    // Utilise la commande LoadMovie.
    //--------------------------------------------
    public class CommandLoadMovieInScreen : IUndoableCommand
    {
        private readonly string _filePath;
        private readonly int _forceScreen;
        private readonly ScreenManagerKernel _screenManagerKernel;

        #region constructor

        public CommandLoadMovieInScreen(ScreenManagerKernel smk, string _filePath, int iForceScreen, bool bStoreState)
        {
            _screenManagerKernel = smk;
            this._filePath = _filePath;
            _forceScreen = iForceScreen;
            if (bStoreState)
            {
                _screenManagerKernel.StoreCurrentState();
            }
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandLoadMovieInScreen_FriendlyName; }
        }

        /// <summary>
        ///     Execution de la commande
        ///     Si ok, enregistrement du path dans l'historique.
        /// </summary>
        public void Execute()
        {
            //-----------------------------------------------------------------------------------------------
            // Principes d'ouverture.
            //
            // 1. Si il n'y a qu'un seul écran, on ouvre sur place.
            //      On part du principe que l'utilisateur peut se placer en mode DualScreen s'il le souhaite.
            //      Sinon on doit demander si il veut charger sur place ou pas...
            //      Si c'est un écran Capture -> idem.
            //      On offre de plus la possibilité d'annuler l'action au cas où.
            //
            // 2. Si il y a deux players, dont au moins un vide, on ouvre dans le premier vide trouvé.
            //
            // 3. Si il y a deux players plein, on pose à droite.
            //
            // 4+ Variations à définir...
            // 4. Si il y a 1 player plein et un capture vide, on ouvre dans le player.
            //-----------------------------------------------------------------------------------------------
            ICommand clm;
            var cm = CommandManager.Instance();
            ICommand css = new CommandShowScreens(_screenManagerKernel);

            if (_forceScreen != -1)
            {
                // Position d'écran forcée: Vérifier s'il y a des choses à enregistrer.

                PlayerScreen ps = (PlayerScreen)_screenManagerKernel.ScreenList[_forceScreen - 1];
                var bLoad = true;
                if (ps.FrameServer.Metadata.IsDirty)
                {
                    var dr = ConfirmDirty();
                    if (dr == DialogResult.Yes)
                    {
                        // Launch the save dialog.
                        // Note: if we cancel this one, we will go on without saving...
                        _screenManagerKernel.MnuSaveOnClick(null, EventArgs.Empty);
                    }
                    else if (dr == DialogResult.Cancel)
                    {
                        // Cancel the load.
                        bLoad = false;
                        _screenManagerKernel.CancelLastCommand = true;
                    }
                    // else (DialogResult.No) => Do nothing.
                }

                if (bLoad)
                {
                    // Utiliser l'écran, qu'il soit vide ou plein.
                    clm = new CommandLoadMovie(ps, _filePath);
                    CommandManager.LaunchCommand(clm);

                    //Si on a pu charger la vidéo, sauver dans l'historique
                    if (ps.FrameServer.VideoFile.Loaded)
                    {
                        SaveFileToHistory(_filePath);
                    }
                }
            }
            else
            {
                switch (_screenManagerKernel.ScreenList.Count)
                {
                    case 0:
                        {
                            // Ajouter le premier écran
                            ICommand caps = new CommandAddPlayerScreen(_screenManagerKernel, false);
                            CommandManager.LaunchCommand(caps);

                            // Charger la vidéo dedans
                            PlayerScreen ps = _screenManagerKernel.ScreenList[0] as PlayerScreen;
                            if (ps != null)
                            {
                                clm = new CommandLoadMovie(ps, _filePath);
                                CommandManager.LaunchCommand(clm);

                                //Si on a pu charger la vidéo, sauver dans l'historique
                                if (ps.FrameServer.VideoFile.Loaded)
                                {
                                    SaveFileToHistory(_filePath);
                                }

                                //Afficher l'écran qu'on vient de le créer.
                                CommandManager.LaunchCommand(css);
                            }
                            break;
                        }
                    case 1:
                        {
                            PlayerScreen ps = _screenManagerKernel.ScreenList[0] as PlayerScreen;
                            if (ps != null)
                            {
                                var bLoad = true;
                                if (ps.FrameServer.Metadata.IsDirty)
                                {
                                    var dr = ConfirmDirty();
                                    if (dr == DialogResult.Yes)
                                    {
                                        // Launch the save dialog.
                                        // Note: if we cancel this one, we will go on without saving...
                                        _screenManagerKernel.MnuSaveOnClick(null, EventArgs.Empty);
                                    }
                                    else if (dr == DialogResult.Cancel)
                                    {
                                        // Cancel the load.
                                        bLoad = false;
                                        _screenManagerKernel.CancelLastCommand = true;
                                    }
                                    // else (DialogResult.No) => Do nothing.
                                }

                                if (bLoad)
                                {
                                    clm = new CommandLoadMovie(ps, _filePath);
                                    CommandManager.LaunchCommand(clm);

                                    //Si on a pu charger la vidéo, sauver dans l'historique
                                    if (ps.FrameServer.VideoFile.Loaded)
                                    {
                                        SaveFileToHistory(_filePath);
                                    }
                                }
                            }
                            else
                            {
                                // Only screen is a capture screen and we try to play a video.
                                // In that case we create a new player screen and load the video in it.

                                ICommand caps = new CommandAddPlayerScreen(_screenManagerKernel, false);
                                CommandManager.LaunchCommand(caps);

                                // Reset the buffer before the video is loaded.
                                _screenManagerKernel.UpdateCaptureBuffers();

                                // load video.
                                PlayerScreen newScreen = (_screenManagerKernel.ScreenList.Count > 0)
                                    ? (_screenManagerKernel.ScreenList[1] as PlayerScreen)
                                    : null;
                                if (newScreen != null)
                                {
                                    clm = new CommandLoadMovie(newScreen, _filePath);
                                    CommandManager.LaunchCommand(clm);

                                    //video loaded finely, save in history.
                                    if (newScreen.FrameServer.VideoFile.Loaded)
                                    {
                                        SaveFileToHistory(_filePath);
                                    }

                                    // Display screens.
                                    CommandManager.LaunchCommand(css);
                                }
                            }

                            break;
                        }
                    case 2:
                        {
                            //Chercher un écran vide.
                            var iEmptyScreen = -1;

                            PlayerScreen ps0 = _screenManagerKernel.ScreenList[0] as PlayerScreen;
                            PlayerScreen ps1 = _screenManagerKernel.ScreenList[1] as PlayerScreen;

                            if (ps0 != null && ps0.FrameServer.VideoFile.Loaded == false)
                            {
                                iEmptyScreen = 0;
                            }
                            else if (ps1 != null && ps1.FrameServer.VideoFile.Loaded == false)
                            {
                                iEmptyScreen = 1;
                            }

                            if (iEmptyScreen >= 0)
                            {
                                // On a trouvé un écran vide, charger la vidéo dedans.
                                clm = new CommandLoadMovie((PlayerScreen)_screenManagerKernel.ScreenList[iEmptyScreen],
                                    _filePath);
                                CommandManager.LaunchCommand(clm);

                                //Si on a pu charger la vidéo, sauver dans l'historique
                                if (
                                    ((PlayerScreen)_screenManagerKernel.ScreenList[iEmptyScreen]).FrameServer.VideoFile
                                        .Loaded)
                                {
                                    SaveFileToHistory(_filePath);
                                }

                                //--------------------------------------------
                                // Sur échec, on ne modifie pas l'écran actif.
                                // normalement c'est toujours l'autre écran.
                                //--------------------------------------------
                            }
                            else
                            {
                                // On a pas trouvé d'écran vide...
                                // Par défaut : toujours à droite.
                                // (étant donné que l'utilisateur à la possibilité d'annuler l'opération
                                // et de revenir à l'ancienne vidéo facilement, autant éviter une boîte de dialogue.)

                                PlayerScreen ps = _screenManagerKernel.ScreenList[1] as PlayerScreen;
                                if (ps != null)
                                {
                                    var bLoad = true;
                                    if (ps.FrameServer.Metadata.IsDirty)
                                    {
                                        var dr = ConfirmDirty();
                                        if (dr == DialogResult.Yes)
                                        {
                                            // Launch the save dialog.
                                            // Note: if we cancel this one, we will go on without saving...
                                            _screenManagerKernel.MnuSaveOnClick(null, EventArgs.Empty);
                                        }
                                        else if (dr == DialogResult.Cancel)
                                        {
                                            // Cancel the load.
                                            bLoad = false;
                                            _screenManagerKernel.CancelLastCommand = true;
                                        }
                                        // else (DialogResult.No) => Do nothing.
                                    }

                                    if (bLoad)
                                    {
                                        clm = new CommandLoadMovie(ps, _filePath);
                                        CommandManager.LaunchCommand(clm);

                                        //Si on a pu charger la vidéo, sauver dans l'historique
                                        if (ps.FrameServer.VideoFile.Loaded)
                                        {
                                            SaveFileToHistory(_filePath);
                                        }
                                        else
                                        {
                                            //----------------------------------------------------------------------------
                                            // Echec de chargement, vérifier si on ne vient pas d'invalider l'écran actif.
                                            //----------------------------------------------------------------------------
                                            if (_screenManagerKernel.MActiveScreen == ps)
                                            {
                                                _screenManagerKernel.Screen_SetActiveScreen(
                                                    _screenManagerKernel.ScreenList[0]);
                                            }
                                        }
                                    }
                                }
                            }

                            // Vérifier qu'on a un écran actif.
                            // sinon, positionner le premier comme actif.
                            break;
                        }
                    default:
                        break;
                }
            }

            _screenManagerKernel.OrganizeCommonControls();
            _screenManagerKernel.OrganizeMenus();
            _screenManagerKernel.UpdateStatusBar();
        }

        public void Unexecute()
        {
            _screenManagerKernel.RecallState();
        }

        private void SaveFileToHistory(string _FilePath)
        {
            // Enregistrer le nom du fichier dans l'historique.
            var pm = PreferencesManager.Instance();
            pm.HistoryAdd(_FilePath);
            pm.OrganizeHistoryMenu();
        }

        private DialogResult ConfirmDirty()
        {
            return MessageBox.Show(ScreenManagerLang.InfoBox_MetadataIsDirty_Text.Replace("\\n", "\n"),
                ScreenManagerLang.InfoBox_MetadataIsDirty_Title,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }
    }
}