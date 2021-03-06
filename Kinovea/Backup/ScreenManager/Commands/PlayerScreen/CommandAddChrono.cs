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
using System;
using System.Reflection;
using System.Resources;
using System.Threading;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    public class CommandAddChrono : IUndoableCommand
    {

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandAddChrono_FriendlyName; }
        }
        
        private Action m_DoInvalidate;
        private Action m_DoUndrawn;
        private Metadata m_Metadata;
        private DrawingChrono m_Chrono;

        #region constructor
        public CommandAddChrono(Action _invalidate, Action _undrawn, Metadata _Metadata)
        {
            m_DoInvalidate = _invalidate;
        	m_DoUndrawn = _undrawn;
            m_Metadata = _Metadata;
            m_Chrono = m_Metadata.ExtraDrawings[m_Metadata.SelectedExtraDrawing] as DrawingChrono;
        }
        #endregion

        public void Execute()
        {
        	// In the case of the first execution, the Chrono has already been added to the extra drawings list.
        	if(m_Chrono != null)
        	{
        		if(m_Metadata.ExtraDrawings.IndexOf(m_Chrono) == -1)
        		{
        			m_Metadata.AddChrono(m_Chrono);
        			m_DoInvalidate();
        		}
        	}
        }
        public void Unexecute()
        {
            // Delete this chrono.
            if(m_Chrono != null)
            {
            	m_Metadata.ExtraDrawings.Remove(m_Chrono);
            	m_DoUndrawn();
                m_DoInvalidate();
            }
        }
    }
}

