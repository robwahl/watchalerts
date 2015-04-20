#region License

/*
Copyright © Joan Charmant 2010.
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

#endregion License

using Kinovea.Services;
using System.Collections.Generic;
using System.Drawing;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     AbstractDevice, a class to represent a device identification.
    /// </summary>
    public class DeviceDescriptor
    {
        public DeviceCapability GetBestSizeCapability()
        {
            var bestCap = Capabilities[0];
            var maxPixels = bestCap.NumberOfPixels;

            for (var i = 1; i < Capabilities.Count; i++)
            {
                if (Capabilities[i].NumberOfPixels > maxPixels)
                {
                    bestCap = Capabilities[i];
                    maxPixels = bestCap.NumberOfPixels;
                }
            }

            return bestCap;
        }

        public DeviceCapability GetCapabilityFromSpecs(DeviceCapability _cap)
        {
            DeviceCapability matchCap = null;

            foreach (var cap in Capabilities)
            {
                if (cap.Equals(_cap))
                {
                    matchCap = cap;
                }
            }

            return matchCap;
        }

        public override string ToString()
        {
            return Name;
        }

        #region Properties

        public string Name { get; }

        public string Identification { get; }

        public List<DeviceCapability> Capabilities { get; set; } = new List<DeviceCapability>();

        public DeviceCapability SelectedCapability { get; set; } = private new DeviceCapability(Size.Empty, 0);

        public string NetworkCameraUrl { get; set; } = "";

        public NetworkCameraFormat NetworkCameraFormat { get; set; }

        public bool Empty { get; }

        public bool Network { get; set; }

        #endregion Properties



        #region Constructor

        public DeviceDescriptor(string name, string identification)
        {
            // Constructor for capture devices.
            Name = name;
            Identification = identification;
        }

        public DeviceDescriptor(string name, string url, NetworkCameraFormat format)
        {
            // Constructor for network devices.
            Name = name;
            NetworkCameraUrl = url;
            NetworkCameraFormat = format;
            Network = true;
        }

        public DeviceDescriptor(string name)
        {
            // Constructor for empty devices.
            Name = name;
            Empty = true;
        }

        #endregion Constructor
    }
}