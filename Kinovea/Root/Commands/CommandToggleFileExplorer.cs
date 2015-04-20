using Kinovea.Root.Languages;
using Kinovea.Services;
using System.Windows.Forms;

namespace Kinovea.Root.Commands
{
    public class CommandToggleFileExplorer : IUndoableCommand
    {
        #region constructor

        public CommandToggleFileExplorer(SplitContainer splitter, ToolStripMenuItem menuItem)
        {
            _splitter = splitter;
            _menuItem = menuItem;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return RootLang.CommandToggleFileExplorer_FriendlyName; }
        }

        public void Execute()
        {
            if (_splitter.Panel1Collapsed)
            {
                _splitter.Panel1Collapsed = false;
                _menuItem.Checked = true;
            }
            else
            {
                _splitter.Panel1Collapsed = true;
                _menuItem.Checked = false;
            }
        }

        public void Unexecute()
        {
            //Annuler correspond à refaire un Toggle.
            Execute();
        }

        #region Members

        private readonly SplitContainer _splitter;
        private readonly ToolStripMenuItem _menuItem;

        #endregion Members
    }
}