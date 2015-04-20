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
    //--------------------------------------------
    // CommandAddPlayerScreen -> devrait être réversible ?
    // Charge le fichier spécifier dans un écran, en créé un si besoin.
    // Si ok, réorganise les écrans pour montrer le nouveau ou décharger un ancien si besoin
    // Affiche le nouvel écran avec la vidéo dedans, prête.
    //--------------------------------------------
    public class CommandDeleteKeyframe : IUndoableCommand
    {
        private readonly long _mIFramePosition;
        private readonly Keyframe _mKeyframe;
        private readonly Metadata _mMetadata;
        private readonly PlayerScreenUserInterface _mPsui;

        #region constructor

        public CommandDeleteKeyframe(PlayerScreenUserInterface psui, Metadata metadata, long iFramePosition)
        {
            _mPsui = psui;
            _mIFramePosition = iFramePosition;
            _mMetadata = metadata;

            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                _mKeyframe = _mMetadata[iIndex];
            }
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandDeleteKeyframe_FriendlyName; }
        }

        /// <summary>
        ///     Execution de la commande
        /// </summary>
        public void Execute()
        {
            // Delete a Keyframe at given position
            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                _mPsui.OnRemoveKeyframe(iIndex);
            }
        }

        public void Unexecute()
        {
            // Re Add the Keyframe
            _mPsui.OnAddKeyframe(_mIFramePosition);

            // Re Add all drawings on the frame
            // We can't add them through the CommandAddDrawing scheme,
            // because it completely messes up the Commands History.

            // Even now, Command History is quite messed up, but the user need to
            // go back and forth in the undo/redo to notice the problem.

            if (_mKeyframe.Drawings.Count > 0)
            {
                var iIndex = GetKeyframeIndex();
                var cm = CommandManager.Instance();

                for (var i = _mKeyframe.Drawings.Count - 1; i >= 0; i--)
                {
                    // 1. Add the drawing to the Keyframe
                    _mMetadata[iIndex].Drawings.Insert(0, _mKeyframe.Drawings[i]);

                    // 2. Call the Command
                    //IUndoableCommand cad = new CommandAddDrawing(m_psui, m_Metadata, m_iFramePosition);
                    //cm.LaunchUndoableCommand(cad);
                }

                // We need to block the Redo here.
                // In normal behavior, we should have a "Redo : Delete Keyframe",
                // But here we added other commands, so we'll discard commands that are up in the m_CommandStack.
                // To avoid having a "Redo : Add Drawing" that makes no sense.
                //cm.BlockRedo();
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