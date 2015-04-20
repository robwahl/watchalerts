//+----------------------------------------------------------------------------
//
// File Name: CommandLineArgumentException.cs
// Description: An expection thrown when a command line argument is missing
//              or some parsing error occurs.
// Author: Ferad Zyulkyarov ferad.zyulkyarov[@]bsc.es
// Date: 04.02.2008
// License: LGPL
//
//-----------------------------------------------------------------------------

using System;

namespace Kinovea.Services
{
    /// <summary>
    ///     An expection thrown when a command line argument is missing or some
    ///     parsing error occurs.
    /// </summary>
    [Serializable]
    internal class CommandLineArgumentException : Exception
    {
        public string ErrorMessage { get; set; }

        public CommandLineArgumentException(string errorMessage)
        {
            if (errorMessage == null) throw new ArgumentNullException("errorMessage");
            ErrorMessage = errorMessage;
            // TODO: Complete member initialization
        }
    }
}