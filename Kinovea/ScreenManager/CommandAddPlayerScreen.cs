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
    public class CommandAddPlayerScreen : IUndoableCommand
    {
        private readonly ScreenManagerKernel _mScreenManagerKernel;

        #region constructor

        public CommandAddPlayerScreen(ScreenManagerKernel smk, bool bStoreState)
        {
            _mScreenManagerKernel = smk;
            if (bStoreState)
            {
                _mScreenManagerKernel.StoreCurrentState();
            }
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandAddPlayerScreen_FriendlyName; }
        }

        /// <summary>
        ///     Add a PlayerScreen to the screen list and initialize it.
        /// </summary>
        public void Execute()
        {
            PlayerScreen screen = new PlayerScreen(_mScreenManagerKernel);
            screen.RefreshUiCulture();
            _mScreenManagerKernel.ScreenList.Add(screen);
        }

        public void Unexecute()
        {
            _mScreenManagerKernel.RecallState();
        }
    }
}