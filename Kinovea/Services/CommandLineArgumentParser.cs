//+----------------------------------------------------------------------------
//
// File Name: CommandLineArgumentParser.cs
// Description: A command line argument parser.
// Author: Ferad Zyulkyarov ferad.zyulkyarov[@]bsc.es
// Date: 04.02.2008
// License: LGPL.
//
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace Kinovea.Services
{
    /// <summary>
    ///     Command line argument parser.
    ///     Usage:
    ///     1. Make a reference file Bsc.CommandLineArgumentParser.dll.
    ///     In solution explorer, right click on "References" and select "Add Reference"
    ///     from the context menu. Then click on the "Browse" tab and navigate to
    ///     Bsc.CommandLineArgumentParser.dll.
    ///     2. Include the Bsc namespace.
    ///     using Bsc.
    /// </summary>
    /// <example>
    ///     See ExampleUsage.cs
    /// </example>
    public class CommandLineArgumentParser
    {
        public const string Version = "1.0.0";

        /// <summary>
        ///     The character used to distinguish which command lines parameters.
        /// </summary>
        public const string ParamSeparator = "-";

        /// <summary>
        ///     Stores the name=value required parameters.
        /// </summary>
        private static Dictionary<string, string> _requiredParameters;

        /// <summary>
        ///     Optional parameters.
        /// </summary>
        private static Dictionary<string, string> _optionalParameters;

        /// <summary>
        ///     Stores the list of the supported switches.
        /// </summary>
        private static Dictionary<string, bool> _switches;

        /// <summary>
        ///     Store the list of missing required parameters.
        /// </summary>
        private static List<string> _missingRequiredParameters;

        /// <summary>
        ///     Store the list of missing values of parameters.
        /// </summary>
        private static List<string> _missingValue;

        /// <summary>
        ///     Contains the raw arguments.
        /// </summary>
        private static List<string> _rawArguments;

        /// <summary>
        ///     Define the required parameters that the user of the program
        ///     must provide.
        /// </summary>
        /// <param name="requiredParameterNames">
        ///     The list of the required parameters.
        /// </param>
        /// <exception cref="CommandLineArgumentException"></exception>
        public static void DefineRequiredParameters(string[] requiredParameterNames)
        {
            _requiredParameters = new Dictionary<string, string>();

            foreach (var param in requiredParameterNames)
            {
                var temp = param;
                if (temp != null) temp.Trim();
                if (string.IsNullOrEmpty(param))
                {
                    var errorMessage = "Error: The required command line parameter '" + param + "' is empty.";
                    throw new CommandLineArgumentException(errorMessage);
                }

                _requiredParameters.Add(param, string.Empty);
            }
        }

        /// <summary>
        ///     Define the optional parameters. The parameters must be provided with their
        ///     default values in the following format "paramName=paramValue".
        /// </summary>
        /// <param name="optionalParameters">
        ///     The list of the optional parameters with their default values.
        /// </param>
        /// <exception cref="CommandLineArgumentException"></exception>
        public static void DefineOptionalParameter(string[] optionalParameters)
        {
            _optionalParameters = new Dictionary<string, string>();

            foreach (var param in optionalParameters)
            {
                var tokens = param.Split('=');

                if (tokens.Length != 2)
                {
                    var errorMessage = "Error: The optional command line parameter '" + param +
                                       "' has wrong format.\n Expeted param=value.";
                    throw new CommandLineArgumentException(errorMessage);
                }

                tokens[0] = tokens[0].Trim();
                if (string.IsNullOrEmpty(tokens[0]))
                {
                    var errorMessage = "Error: The optional command line parameter '" + param + "' has empty name.";
                    throw new CommandLineArgumentException(errorMessage);
                }

                tokens[1] = tokens[1].Trim();
                if (string.IsNullOrEmpty(tokens[1]))
                {
                }

                _optionalParameters.Add(tokens[0], tokens[1]);
            }
        }

        /// <summary>
        ///     Define the optional parameters. The parameters must be provided with their
        ///     default values.
        /// </summary>
        /// <param name="optionalParameters">
        ///     The list of the optional parameters with their default values.
        /// </param>
        /// <exception cref="CommandLineArgumentException"></exception>
        public static void DefineOptionalParameter(KeyValuePair<string, string>[] optionalParameters)
        {
            _optionalParameters = new Dictionary<string, string>();

            foreach (var param in optionalParameters)
            {
                var key = param.Key;
                key = key.Trim();

                var value = param.Value;
                value = value.Trim();

                if (string.IsNullOrEmpty(key))
                {
                    var errorMessage = "Error: The name of the optional parameter '" + param.Key + "' is empty.";
                    throw new CommandLineArgumentException(errorMessage);
                }

                if (string.IsNullOrEmpty(value))
                {
                    var errorMessage = "Error: The value of the optional parameter '" + param.Key + "' is empty.";
                    throw new CommandLineArgumentException(errorMessage);
                }

                _optionalParameters.Add(param.Key, param.Value);
            }
        }

        /// <summary>
        ///     Defines the supported command line switches. Switch is a parameter
        ///     without value. When provided it is used to switch on a given feature or
        ///     functionality provided by the application. For example a switch for tracing.
        /// </summary>
        /// <param name="switches"></param>
        /// <exception cref="CommandLineArgumentException"></exception>
        public static void DefineSwitches(string[] switches)
        {
            _switches = new Dictionary<string, bool>(switches.Length);

            foreach (var sw in switches)
            {
                var temp = sw;
                temp = temp.Trim();

                if (string.IsNullOrEmpty(temp))
                {
                    var errorMessage = "Error: The switch '" + sw + "' is empty.";
                    throw new CommandLineArgumentException(errorMessage);
                }

                _switches.Add(temp, false);
            }
        }

        /// <summary>
        ///     Parse the command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void ParseArguments(string[] args)
        {
            // All arguments are unknown (raw) until matched.
            _rawArguments = new List<string>(args);

            _missingRequiredParameters = new List<string>();
            _missingValue = new List<string>();

            ParseRequiredParameters();
            ParseOptionalParameters();
            ParseSwitches();

            ThrowIfErrors();
        }

        /// <summary>
        ///     Returns the value of the specified parameter.
        /// </summary>
        /// <param name="paramName">The name of the perameter.</param>
        /// <exception cref="CommandLineArgumentException"></exception>
        /// <returns>The value of the perameter.</returns>
        public static string GetParamValue(string paramName)
        {
            string paramValue;

            if (_requiredParameters != null && _requiredParameters.ContainsKey(paramName))
            {
                paramValue = _requiredParameters[paramName];
            }
            else if (_optionalParameters != null && _optionalParameters.ContainsKey(paramName))
            {
                paramValue = _optionalParameters[paramName];
            }
            else
            {
                var errorMessage = "Error: The paramter '" + paramName + "' is not supported.";
                throw new CommandLineArgumentException(errorMessage);
            }

            return paramValue;
        }

        public static bool IsSwitchOn(string switchName)
        {
            bool switchValue;

            if (_switches != null && _switches.ContainsKey(switchName))
            {
                switchValue = _switches[switchName];
            }
            else
            {
                var errorMessage = "Error: switch '" + switchName + "' not supported.";
                throw new CommandLineArgumentException(errorMessage);
            }

            return switchValue;
        }

        private static void ParseRequiredParameters()
        {
            if (_requiredParameters == null || _requiredParameters.Count == 0)
            {
                return;
            }

            var paramNames = new List<string>(_requiredParameters.Keys);

            foreach (var paramName in paramNames)
            {
                var paramInd = _rawArguments.IndexOf(paramName);
                if (paramInd < 0)
                {
                    _missingRequiredParameters.Add(paramName);
                }
                else
                {
                    if (paramInd + 1 < _rawArguments.Count)
                    {
                        //
                        // The argument after the parameter name is expected to be its value.
                        // No check for error is done here.
                        //
                        _requiredParameters[paramName] = _rawArguments[paramInd + 1];

                        _rawArguments.RemoveAt(paramInd);
                        _rawArguments.RemoveAt(paramInd);
                    }
                    else
                    {
                        _missingValue.Add(paramName);
                        _rawArguments.RemoveAt(paramInd);
                    }
                }
            }
        }

        private static void ParseOptionalParameters()
        {
            if (_optionalParameters == null || _optionalParameters.Count == 0)
            {
                return;
            }

            var paramNames = new List<string>(_optionalParameters.Keys);

            foreach (var paramName in paramNames)
            {
                var paramInd = _rawArguments.IndexOf(paramName);

                if (paramInd >= 0)
                {
                    if (paramInd + 1 < _rawArguments.Count)
                    {
                        _optionalParameters[paramName] = _rawArguments[paramInd + 1];

                        _rawArguments.RemoveAt(paramInd);

                        //
                        // After removing the param name, the index of the value
                        // becomes again paramInd.
                        //
                        _rawArguments.RemoveAt(paramInd);
                    }
                    else
                    {
                        _missingValue.Add(paramName);
                        _rawArguments.RemoveAt(paramInd);
                    }
                }
            }
        }

        private static void ParseSwitches()
        {
            if (_switches == null || _switches.Count == 0)
            {
                return;
            }

            var paramNames = new List<string>(_switches.Keys);

            foreach (var paramName in paramNames)
            {
                var paramInd = _rawArguments.IndexOf(paramName);

                if (paramInd >= 0)
                {
                    _switches[paramName] = true;
                    _rawArguments.RemoveAt(paramInd);
                }
            }
        }

        private static void ThrowIfErrors()
        {
            var errorMessage = new StringBuilder();

            if (_missingRequiredParameters.Count > 0 || _missingValue.Count > 0 || _rawArguments.Count > 0)
            {
                errorMessage.Append("Error: Processing Command Line Arguments\n");
            }

            if (_missingRequiredParameters.Count > 0)
            {
                errorMessage.Append("Missing Required Parameters\n");
                foreach (var missingParam in _missingRequiredParameters)
                {
                    errorMessage.Append("\t" + missingParam + "\n");
                }
            }

            if (_missingValue.Count > 0)
            {
                errorMessage.Append("Missing Values\n");
                foreach (var value in _missingValue)
                {
                    errorMessage.Append("\t" + value + "\n");
                }
            }

            if (_rawArguments.Count > 0)
            {
                errorMessage.Append("Unknown Parameters");
                foreach (var unknown in _rawArguments)
                {
                    errorMessage.Append("\t" + unknown + "\n");
                }
            }

            if (errorMessage.Length > 0)
            {
                throw new CommandLineArgumentException(errorMessage.ToString());
            }
        }
    }
}