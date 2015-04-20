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

namespace Kinovea.ScreenManager
{
    public class CommandModifyChrono : IUndoableCommand
    {
        #region constructor

        public CommandModifyChrono(PlayerScreenUserInterface psui, Metadata metadata, ChronoModificationType modifType,
            long newValue)
        {
            // In the special case of Countdown toggle, the new value will be 0 -> false, true otherwise .
            _mPsui = psui;
            _mMetadata = metadata;
            _mChrono = _mMetadata.ExtraDrawings[_mMetadata.SelectedExtraDrawing] as DrawingChrono;
            _mINewValue = newValue;
            _mModifType = modifType;

            // Save old values
            if (_mChrono != null)
            {
                _mIStartCountingTimestamp = _mChrono.TimeStart;
                _mIStopCountingTimestamp = _mChrono.TimeStop;
                _mIInvisibleTimestamp = _mChrono.TimeInvisible;
                _mBCountdown = _mChrono.CountDown;
            }
        }

        #endregion constructor

        public string FriendlyName
        {
            get
            {
                var friendlyName = "";
                switch (_mModifType)
                {
                    case ChronoModificationType.TimeStart:
                        friendlyName = ScreenManagerLang.mnuChronoStart;
                        break;

                    case ChronoModificationType.TimeStop:
                        friendlyName = ScreenManagerLang.mnuChronoStop;
                        break;

                    case ChronoModificationType.TimeHide:
                        friendlyName = ScreenManagerLang.mnuChronoHide;
                        break;

                    case ChronoModificationType.Countdown:
                        friendlyName = ScreenManagerLang.mnuChronoCountdown;
                        break;

                    default:
                        break;
                }
                return friendlyName;
            }
        }

        /// <summary>
        ///     Execution de la commande
        /// </summary>
        public void Execute()
        {
            if (_mChrono != null)
            {
                switch (_mModifType)
                {
                    case ChronoModificationType.TimeStart:
                        _mChrono.Start(_mINewValue);
                        break;

                    case ChronoModificationType.TimeStop:
                        _mChrono.Stop(_mINewValue);
                        break;

                    case ChronoModificationType.TimeHide:
                        _mChrono.Hide(_mINewValue);
                        break;

                    case ChronoModificationType.Countdown:
                        _mChrono.CountDown = (_mINewValue != 0);
                        break;

                    default:
                        break;
                }
            }

            _mPsui.pbSurfaceScreen.Invalidate();
        }

        public void Unexecute()
        {
            // The 'execute' action might have forced a modification on other values. (e.g. stop before start)
            // We must reinject all the old values.
            if (_mChrono != null)
            {
                _mChrono.Start(_mIStartCountingTimestamp);
                _mChrono.Stop(_mIStopCountingTimestamp);
                _mChrono.Hide(_mIInvisibleTimestamp);
                _mChrono.CountDown = _mBCountdown;
            }

            _mPsui.pbSurfaceScreen.Invalidate();
        }

        #region Members

        private readonly PlayerScreenUserInterface _mPsui;
        private readonly Metadata _mMetadata;
        private readonly DrawingChrono _mChrono;

        // New value
        private readonly ChronoModificationType _mModifType;

        private readonly long _mINewValue;

        // Memo
        private readonly long _mIStartCountingTimestamp;

        private readonly long _mIStopCountingTimestamp;
        private readonly long _mIInvisibleTimestamp;
        private readonly bool _mBCountdown;

        #endregion Members
    }
}