using Kinovea.Root.Languages;
using Kinovea.Services;
using log4net;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace Kinovea.Root.Commands
{
    internal class CommandSwitchUiCulture : IUndoableCommand
    {
        internal CommandSwitchUiCulture(RootKernel kernel, Thread thread, CultureInfo ci, CultureInfo oldCi)
        {
            _oldCi = oldCi;
            _ci = ci;
            _thread = thread;
            _kernel = kernel;
        }

        public string FriendlyName
        {
            get { return RootLang.CommandSwitchUICulture_FriendlyName; }
        }

        public void Execute()
        {
            Log.Debug(string.Format("Changing culture from [{0}] to [{1}].", _oldCi.Name, _ci.Name));
            ChangeToCulture(_ci);
        }

        public void Unexecute()
        {
            Log.Debug(string.Format("Changing back culture from [{0}] to [{1}].", _ci.Name, _oldCi.Name));
            ChangeToCulture(_oldCi);
        }

        private void ChangeToCulture(CultureInfo newCulture)
        {
            var pm = PreferencesManager.Instance();
            pm.UiCultureName = newCulture.Name;
            _thread.CurrentUICulture = pm.GetSupportedCulture();
            _kernel.RefreshUiCulture();
            pm.Export();
        }

        #region Members

        private readonly CultureInfo _ci;
        private readonly CultureInfo _oldCi;
        private readonly Thread _thread;
        private readonly RootKernel _kernel;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members
    }
}