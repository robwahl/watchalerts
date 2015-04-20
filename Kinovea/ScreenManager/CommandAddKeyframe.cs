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
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    //--------------------------------------------
    // CommandAddPlayerScreen -> devrait �tre r�versible ?
    // Charge le fichier sp�cifier dans un �cran, en cr�� un si besoin.
    // Si ok, r�organise les �crans pour montrer le nouveau ou d�charger un ancien si besoin
    // Affiche le nouvel �cran avec la vid�o dedans, pr�te.
    //--------------------------------------------
    public class CommandAddKeyframe : IUndoableCommand
    {
        private readonly long _mIFramePosition;
        private readonly Metadata _mMetadata;
        private readonly PlayerScreenUserInterface _mPsui;

        #region constructor

        public CommandAddKeyframe(PlayerScreenUserInterface psui, Metadata metadata, long iFramePosition)
        {
            _mPsui = psui;
            _mIFramePosition = iFramePosition;
            _mMetadata = metadata;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.ToolTip_AddKeyframe; }
        }

        /// <summary>
        ///     Execution de la commande
        /// </summary>
        public void Execute()
        {
            // Add a Keyframe at given position
            _mPsui.OnAddKeyframe(_mIFramePosition);
        }

        public void Unexecute()
        {
            // The PlayerScreen used at execute time may not be valid anymore...
            // The MetaData used at execute time may not be valid anymore...
            // (use case : Add KF + Close screen + undo + undo)

            // Delete Keyframe at given position
            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                _mPsui.OnRemoveKeyframe(iIndex);
            }
        }

        private int GetKeyframeIndex()
        {
            var iIndex = -1;
            for (var i = 0; i < _mMetadata.Count; i++)
            {
                if (_mMetadata[i].Position == _mIFramePosition)
                {
                    iIndex = i;
                }
            }

            return iIndex;
        }
    }
}