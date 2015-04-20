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

using System;
using System.Collections.Generic;
using System.Resources;
using System.Windows.Forms;

[assembly: CLSCompliant(true)]

namespace Kinovea.Services
{
    /// <summary>
    ///     Manages Commands execution and undo/redo mechanics.
    ///     Optionnellement couplé à un menu undo/redo
    /// </summary>
    /// <remarks>Design Pattern : Singleton</remarks>
    public class CommandManager
    {
        #region Members

        private readonly List<IUndoableCommand> _mCommandStack;
        private int _mIPlayHead;
        private bool _bIsEmpty;
        private ToolStripMenuItem _undoMenu;
        private ToolStripMenuItem _redoMenu;
        private static CommandManager _instance;

        #endregion Members

        #region Instance et Ctor

        // Récup de l'instance du singleton.
        public static CommandManager Instance()
        {
            return _instance ?? (_instance = new CommandManager());
        }

        //Constructeur privé.
        private CommandManager()
        {
            _mCommandStack = new List<IUndoableCommand>();
            _mIPlayHead = -1;
            _bIsEmpty = true;
        }

        #endregion Instance et Ctor

        #region Implementation

        public static void LaunchCommand(ICommand command)
        {
            if (command != null)
            {
                command.Execute();
            }
        }

        public void LaunchUndoableCommand(IUndoableCommand command)
        {
            if (command != null)
            {
                // Dépiler ce qui est au dessus de la tête de lecture
                if ((_mCommandStack.Count - 1) > _mIPlayHead)
                {
                    _mCommandStack.RemoveRange(_mIPlayHead + 1, (_mCommandStack.Count - 1) - _mIPlayHead);
                }

                //Empiler la commande
                _mCommandStack.Add(command);
                _mIPlayHead = _mCommandStack.Count - 1;
                _bIsEmpty = false;

                //Executer la commande
                DoCurrentCommand();

                // Mise à jour du menu
                UpdateMenus();
            }
        }

        private void DoCurrentCommand()
        {
            if (!_bIsEmpty)
            {
                _mCommandStack[_mIPlayHead].Execute();
            }
        }

        public void Undo()
        {
            if (!_bIsEmpty)
            {
                //Unexecuter la commande courante.
                _mCommandStack[_mIPlayHead].Unexecute();

                //délpacer la tête de lecture vers le bas.
                _mIPlayHead--;
            }

            // Mettre les menus à jour.
            UpdateMenus();
        }

        public void Redo()
        {
            if ((_mCommandStack.Count - 1) > _mIPlayHead)
            {
                _mIPlayHead++;
                DoCurrentCommand();
                UpdateMenus();
            }
        }

        public void RegisterUndoMenu(ToolStripMenuItem undoMenu)
        {
            if (undoMenu != null)
            {
                _undoMenu = undoMenu;
            }
        }

        public void RegisterRedoMenu(ToolStripMenuItem redoMenu)
        {
            if (redoMenu != null)
            {
                _redoMenu = redoMenu;
            }
        }

        public void ResetHistory()
        {
            _mCommandStack.Clear();
            _mIPlayHead = -1;
            _bIsEmpty = true;
            UpdateMenus();
        }

        public void UnstackLastCommand()
        {
            // This happens when the command is cancelled while being performed.
            // For example, cancellation of screen closing.
            if (_mCommandStack.Count > 0)
            {
                _mCommandStack.RemoveAt(_mCommandStack.Count - 1);
                _mIPlayHead = _mCommandStack.Count - 1;
                _bIsEmpty = (_mCommandStack.Count < 1);
                UpdateMenus();
            }
        }

        public void BlockRedo()
        {
            // Dépiler ce qui est au dessus de la tête de lecture
            // BlockRedo est appelé pendant le unexecute, donc la playhead n'a pas encore été déplacée.
            if ((_mCommandStack.Count - 1) >= _mIPlayHead && _mIPlayHead >= 0)
            {
                _mCommandStack.RemoveRange(_mIPlayHead, _mCommandStack.Count - _mIPlayHead);
            }
        }

        public void UpdateMenus()
        {
            // Since the menus have their very own Resource Manager in the Tag field,
            // we don't need to have a resx file here with the localization of undo redo.
            // This function is public because it accessed by the main kernel when we update preferences.
            if (_undoMenu != null)
            {
                var rm = _undoMenu.Tag as ResourceManager;
                if (_mIPlayHead < 0)
                {
                    _undoMenu.Enabled = false;
                    if (rm != null) _undoMenu.Text = rm.GetString("mnuUndo");
                }
                else
                {
                    _undoMenu.Enabled = true;
                    if (rm != null)
                        _undoMenu.Text = rm.GetString("mnuUndo") + @" : " + _mCommandStack[_mIPlayHead].FriendlyName;
                }
            }

            if (_redoMenu != null)
            {
                if (_undoMenu != null)
                {
                    var rm = _undoMenu.Tag as ResourceManager;
                    if (_mIPlayHead == (_mCommandStack.Count - 1))
                    {
                        _redoMenu.Enabled = false;
                        if (rm != null) _redoMenu.Text = rm.GetString("mnuRedo");
                    }
                    else
                    {
                        _redoMenu.Enabled = true;
                        if (rm != null)
                            _redoMenu.Text = rm.GetString("mnuRedo") + @" : " +
                                             _mCommandStack[_mIPlayHead + 1].FriendlyName;
                    }
                }
            }
        }

        #endregion Implementation
    }
}