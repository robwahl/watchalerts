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
using Kinovea.VideoFiles;
using System;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    //-------------------------------------------------
    // CommandLoadMovie
    //
    // Objet : Rendre le PlayerScreen opérationnel.
    // - Charger un fichier vidéo dans le PlayerServer
    //--------------------------------------------------

    public class CommandLoadMovie : ICommand
    {
        #region constructor

        public CommandLoadMovie(PlayerScreen playerScreen, string filePath)
        {
            _mPlayerScreen = playerScreen;
            _mFilePath = filePath;
        }

        #endregion constructor

        #region Properties

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandLoadMovie_FriendlyName; }
        }

        #endregion Properties

        /// <summary>
        ///     Command execution.
        ///     Load the given file in the given screen.
        /// </summary>
        public void Execute()
        {
            var dp = DelegatesPool.Instance();
            dp.StopPlaying?.Invoke();

            DirectLoad();
        }

        private void DirectLoad()
        {
            if (_mPlayerScreen.FrameServer.Loaded)
            {
                _mPlayerScreen.MPlayerScreenUi.ResetToEmptyState();
            }

            LoadResult res = _mPlayerScreen.FrameServer.Load(_mFilePath);

            switch (res)
            {
                case LoadResult.Success:
                    {
                        // Try to load first frame and other inits.
                        int iPostLoadProcess = _mPlayerScreen.MPlayerScreenUi.PostLoadProcess();

                        switch (iPostLoadProcess)
                        {
                            case 0:
                                // Loading succeeded.
                                // We already switched to analysis mode if possible.
                                _mPlayerScreen.MPlayerScreenUi.EnableDisableActions(true);
                                break;

                            case -1:
                                {
                                    // Loading the first frame failed.
                                    _mPlayerScreen.MPlayerScreenUi.ResetToEmptyState();
                                    DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_InconsistantMovieError);
                                    break;
                                }
                            case -2:
                                {
                                    // Loading first frame showed that the file is, in the end, not supported.
                                    _mPlayerScreen.MPlayerScreenUi.ResetToEmptyState();
                                    DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_InconsistantMovieError);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                case LoadResult.FileNotOpenned:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_FileNotOpened);
                        break;
                    }
                case LoadResult.StreamInfoNotFound:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_StreamInfoNotFound);
                        break;
                    }
                case LoadResult.VideoStreamNotFound:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_VideoStreamNotFound);
                        break;
                    }
                case LoadResult.CodecNotFound:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_CodecNotFound);
                        break;
                    }
                case LoadResult.CodecNotOpened:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_CodecNotOpened);
                        break;
                    }
                case LoadResult.CodecNotSupported:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_CodecNotSupported);
                        break;
                    }
                case LoadResult.Cancelled:
                    {
                        break;
                    }
                case LoadResult.FrameCountError:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_CodecNotFound);
                        break;
                    }

                default:
                    {
                        DisplayErrorAndDisable(ScreenManagerLang.LoadMovie_UnkownError);
                        break;
                    }
            }

            _mPlayerScreen.UniqueId = Guid.NewGuid();
        }

        private void DisplayErrorAndDisable(string error)
        {
            _mPlayerScreen.MPlayerScreenUi.EnableDisableActions(false);

            MessageBox.Show(
                error,
                ScreenManagerLang.LoadMovie_Error,
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
        }

        #region Members

        private readonly string _mFilePath;
        private readonly PlayerScreen _mPlayerScreen;

        #endregion Members
    }
}