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

using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: CLSCompliant(false)]
[assembly: XmlConfigurator(ConfigFile = "LogConf.xml", Watch = true)]

namespace Kinovea.Root
{
    internal static class Program
    {
        public static Mutex Mutex;
        private static readonly string _appGuid = "b049b83e-90f3-4e84-9289-52ee6ea2a9ea";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static bool FirstInstance
        {
            get
            {
                bool bGotMutex;
                Mutex = new Mutex(false, "Local\\" + _appGuid, out bGotMutex);
                return bGotMutex;
            }
        }

        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

            //--------------------------------------------------------
            // Each time the program runs, we try to register a mutex.
            // If it fails, we are already running.
            //--------------------------------------------------------
            if (FirstInstance)
            {
                SanityCheckDirectories();

                Thread.CurrentThread.Name = "Main";

                Log.Debug("Kinovea starting.");
                Log.Debug("Application level initialisations.");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Log.Debug("Show SplashScreen.");
                var splashForm = new FormSplashScreen();
                splashForm.Show();
                splashForm.Update();

                var kernel = new RootKernel();
                kernel.Prepare();

                Log.Debug("Close splash screen.");
                splashForm.Close();

                Log.Debug("Launch.");
                kernel.Launch();
            }
        }

        private static void SanityCheckDirectories()
        {
            // Create the Kinovea folder under App Data if it doesn't exist.
            var prefDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";
            if (!Directory.Exists(prefDir))
            {
                Directory.CreateDirectory(prefDir);
            }

            // Create the Kinovea\ColorProfiles if it doesn't exist.
            var colDir = prefDir + "ColorProfiles\\";
            if (!Directory.Exists(colDir))
            {
                Directory.CreateDirectory(colDir);
            }
        }

        private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;

            //Dump Exception in a dedicated file.
            var prefDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";
            var name = string.Format("Unhandled Crash - {0}.txt", Guid.NewGuid());

            var message = string.Format("Message: {0}", ex.Message);
            var source = string.Format("Source: {0}", ex.Source);
            var target = string.Format("Target site: {0}", ex.TargetSite);
            var inner = string.Format("InnerException: {0}", ex.InnerException);
            var trace = string.Format("Stack: {0}", ex.StackTrace);

            using (var sw = File.AppendText(prefDir + name))
            {
                sw.WriteLine(message);
                sw.WriteLine(source);
                sw.WriteLine(target);
                sw.WriteLine(inner);
                sw.WriteLine(trace);
                sw.Close();
            }

            // Dump again in the log.
            Log.Error("Unhandled Crash -------------------------");
            Log.Error(message);
            Log.Error(source);
            Log.Error(target);
            Log.Error(inner);
            Log.Error(trace);
        }
    }
}