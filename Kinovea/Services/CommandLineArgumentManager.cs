/*
Copyright © Joan Charmant 2009.
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
using System;
using System.IO;
using System.Reflection;

namespace Kinovea.Services
{
    /// <summary>
    ///     Manages Command line arguments mechanics.
    /// </summary>
    /// <remarks>Design Pattern : Singleton</remarks>
    public class CommandLineArgumentManager
    {
        #region Properties

        public bool ParametersParsed { get; private set; }

        public string InputFile
        {
            get
            {
                if (_mInputFile == NoInputFile)
                    return null;
                return _mInputFile;
            }
        }

        public int SpeedPercentage
        {
            get { return _mISpeedPercentage; }
        }

        public bool SpeedConsumed
        { // Indicates whether the SpeedPercentage argument has been used by a PlayerScreen.
            get;
            set;
        }

        public bool StretchImage { get; private set; }

        public bool HideExplorer { get; private set; }

        #endregion Properties

        #region Members

        private static readonly string NoInputFile = "none";
        private string _mInputFile = NoInputFile;
        private int _mISpeedPercentage = 100;
        private static CommandLineArgumentManager _instance;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Instance and Ctor

        public static CommandLineArgumentManager Instance()
        {
            // get singleton instance.
            return _instance ?? (_instance = new CommandLineArgumentManager());
        }

        private CommandLineArgumentManager()
        {
        }

        #endregion Instance and Ctor

        #region Implementation

        public void InitializeCommandLineParser()
        {
            // Define parameters and switches. (All optional).

            // Parameters and their default values.
            CommandLineArgumentParser.DefineOptionalParameter(
                new[]
                {
                    "-file = " + _mInputFile,
                    "-speed = " + _mISpeedPercentage
                });

            // Switches. (default will be "false")
            CommandLineArgumentParser.DefineSwitches(
                new[]
                {
                    "-stretch",
                    "-noexp"
                });
        }

        public void ParseArguments()
        {
            ParseArguments(new string[] { });
        }

        public void ParseArguments(string[] args)
        {
            args = new string[] { };
            if (args == null) throw new ArgumentNullException("args");
            // Log argumets.
            if (args.Length > 1)
            {
                Log.Debug("Command line arguments:");
                foreach (var arg in args)
                {
                    Log.Debug(arg);
                }
            }

            // Remove first argument (name of the executable) before parsing.
            args = new string[args.Length - 1];
            for (var i = 1; i < args.Length; i++)
            {
                args[i - 1] = args[i];
            }

            try
            {
                // Check for the special case where the only argument is a filename.
                // this happens when you drag a video on kinovea.exe
                if (args.Length == 1)
                {
                    if (File.Exists(args[0]))
                    {
                        _mInputFile = args[0];
                    }
                }
                else if (args.Length == 1 && (args[0].Trim() == "-help" || args[0].Trim() == "-h"))
                {
                    // Check for the special parameter -help or -h,
                    // and then output info on supported params.
                    PrintUsage();
                }
                else
                {
                    CommandLineArgumentParser.ParseArguments(args);

                    // Reparse the types, (we do that in the try catch block in case it fails.)
                    _mInputFile = CommandLineArgumentParser.GetParamValue("-file");
                    _mISpeedPercentage = int.Parse(CommandLineArgumentParser.GetParamValue("-speed"));
                    if (_mISpeedPercentage > 200) _mISpeedPercentage = 200;
                    if (_mISpeedPercentage < 1) _mISpeedPercentage = 1;
                    StretchImage = CommandLineArgumentParser.IsSwitchOn("-stretch");
                    HideExplorer = CommandLineArgumentParser.IsSwitchOn("-noexp");
                }
            }
            catch (CommandLineArgumentException e)
            {
                Log.Error("Command line arguments couldn't be parsed.");
                Log.Error(e.Message);
                PrintUsage();
            }

            // Validate parameters.
            // Here maybe we should check for the coherence of what the user entered.
            // for exemple if he entered a -speed but no -file...

            ParametersParsed = true;
        }

        private static void PrintUsage()
        {
            // Doesn't work ?
            Console.WriteLine();
            Console.WriteLine(@"USAGE:");
            Console.WriteLine(@"kinovea.exe");
            Console.WriteLine(@"    [-file <path>] [-speed <0-200>] [-noexp] [-stretch]");
            Console.WriteLine();
            Console.WriteLine(@"OPTIONS:");
            Console.WriteLine(@"  -file: complete path of a video to launch; default: 'unknown'.");
            Console.WriteLine(@"  -speed: percentage of original speed to play the video; default: 100.");
            Console.WriteLine(@"  -stretch: The video will be expanded to the screen size; default: false.");
            Console.WriteLine(@"  -noexp: The file explorer will not be visible; default: false.");
            Console.WriteLine();
            Console.WriteLine(@"EXAMPLES:");
            Console.WriteLine(@"1. > kinovea.exe -file test.mkv -speed 50");
            Console.WriteLine();
            Console.WriteLine(@"2. > kinovea.exe -file test.mkv -stretch -noexp");
        }

        #endregion Implementation
    }
}