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

using Kinovea.ScreenManager.Languages;
using Kinovea.ScreenManager.Properties;
using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Description of FormDevicePicker.
    /// </summary>
    public partial class FormDevicePicker : Form
    {
        public FormDevicePicker(List<DeviceDescriptor> devices, DeviceDescriptor currentDevice,
            PropertyPagePrompter promptDevicePropertyPage)
        {
            _mPromptDevicePropertyPage = promptDevicePropertyPage;
            _mCurrentDevice = currentDevice;

            InitializeComponent();

            Text = "   " + ScreenManagerLang.ToolTip_DevicePicker;
            btnApply.Text = ScreenManagerLang.Generic_Apply;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            gpCurrentDevice.Text = ScreenManagerLang.dlgDevicePicker_CurrentDevice;
            gpOtherDevices.Text = ScreenManagerLang.dlgDevicePicker_SelectAnother;
            lblUrl.Text = ScreenManagerLang.dlgDevicePicker_Url;
            lblStreamType.Text = ScreenManagerLang.dlgDevicePicker_Type;
            lblConfig.Text = ScreenManagerLang.Generic_Configuration + " :";
            lblNoConf.Text = ScreenManagerLang.dlgDevicePicker_NoConf;
            btnDeviceProperties.Text = ScreenManagerLang.dlgDevicePicker_DeviceProperties;
            lblNoConf.Top = lblConfig.Top;
            lblNoConf.Left = lblConfig.Right;
            lblUrl.Location = lblConfig.Location;
            cmbUrl.Top = cmbCapabilities.Top - 3;
            cmbUrl.Left = lblCurrentlySelected.Left;
            cmbUrl.Width = cmbOtherDevices.Right - cmbUrl.Left;
            cmbStreamType.Top = btnDeviceProperties.Top - 3;
            cmbStreamType.Left = lblCurrentlySelected.Left;

            // Populate current device.
            if (currentDevice == null)
            {
                // No device. This can happen if there is no capture device connected.
                lblCurrentlySelected.Text = ScreenManagerLang.Capture_CameraNotFound;
            }
            else
            {
                lblCurrentlySelected.Text = currentDevice.Name;
            }

            DisplayConfControls(currentDevice);

            // Populate other devices.
            var selectedDev = 0;
            for (var i = 0; i < devices.Count; i++)
            {
                var dd = devices[i];
                cmbOtherDevices.Items.Add(dd);

                if (currentDevice == null)
                {
                    selectedDev = 0;
                }
                else if (dd.Identification == currentDevice.Identification)
                {
                    selectedDev = i;
                }
            }

            cmbOtherDevices.SelectedIndex = selectedDev;
            gpOtherDevices.Enabled = devices.Count > 1;
        }

        private void DisplayConfControls(DeviceDescriptor currentDevice)
        {
            if (currentDevice != null)
            {
                lblConfig.Visible = !currentDevice.Network;
                lblNoConf.Visible = !currentDevice.Network;
                btnDeviceProperties.Visible = !currentDevice.Network;
                cmbCapabilities.Visible = !currentDevice.Network;

                lblUrl.Visible = currentDevice.Network;
                lblStreamType.Visible = currentDevice.Network;
                cmbUrl.Visible = currentDevice.Network;
                cmbStreamType.Visible = currentDevice.Network;

                if (currentDevice.Network)
                {
                    btnCamcorder.Image = Resources.camera_network2;
                    var pm = PreferencesManager.Instance();

                    // Recently used cameras.
                    cmbUrl.Text = currentDevice.NetworkCameraUrl;
                    if (pm.RecentNetworkCameras.Count > 0)
                    {
                        foreach (var url in pm.RecentNetworkCameras)
                        {
                            cmbUrl.Items.Add(url);
                        }
                    }
                    else
                    {
                        cmbUrl.Items.Add(currentDevice.NetworkCameraUrl);
                    }

                    // Type of streams supported.
                    cmbStreamType.Items.Add("JPEG");
                    cmbStreamType.Items.Add("MJPEG");
                    if (currentDevice.NetworkCameraFormat == NetworkCameraFormat.Jpeg)
                    {
                        cmbStreamType.SelectedIndex = 0;
                    }
                    else if (currentDevice.NetworkCameraFormat == NetworkCameraFormat.Mjpeg)
                    {
                        cmbStreamType.SelectedIndex = 1;
                    }
                    else
                    {
                        currentDevice.NetworkCameraFormat = NetworkCameraFormat.Jpeg;
                        cmbStreamType.SelectedIndex = 0;
                    }
                }
                else
                {
                    btnCamcorder.Image = Resources.camera_selected;

                    var selectedCap = 0;
                    for (var i = 0; i < currentDevice.Capabilities.Count; i++)
                    {
                        var dc = currentDevice.Capabilities[i];
                        cmbCapabilities.Items.Add(dc);
                        if (dc == currentDevice.SelectedCapability)
                        {
                            selectedCap = i;
                        }
                    }

                    if (currentDevice.Capabilities.Count > 0)
                    {
                        cmbCapabilities.SelectedIndex = selectedCap;
                        lblNoConf.Visible = false;
                        cmbCapabilities.Visible = true;
                    }
                    else
                    {
                        lblNoConf.Visible = true;
                        cmbCapabilities.Visible = false;
                    }
                }
            }
            else
            {
                btnCamcorder.Image = Resources.camera_notfound;

                // No device currently selected.
                lblConfig.Visible = false;
                lblNoConf.Visible = false;
                btnDeviceProperties.Visible = false;
                cmbCapabilities.Visible = false;

                lblUrl.Visible = false;
                lblStreamType.Visible = false;
                cmbUrl.Visible = false;
                cmbStreamType.Visible = false;
            }
        }

        private void cmbOtherDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Enable / disable the configuration picker if we change device,
            // so the user doesn't think he could change both at the same time.
            var selected = cmbOtherDevices.Items[cmbOtherDevices.SelectedIndex] as DeviceDescriptor;
            if (_mCurrentDevice == null)
            {
                gpCurrentDevice.Enabled = false;
            }
            else if (_mCurrentDevice.Network)
            {
                gpCurrentDevice.Enabled = selected.Network;
            }
            else
            {
                gpCurrentDevice.Enabled = !selected.Empty && !selected.Network &&
                                          (selected.Identification == _mCurrentDevice.Identification);
            }
        }

        private void btnDeviceProperties_Click(object sender, EventArgs e)
        {
            // Ask the API to display the device property page.
            // This page is implemented by the driver.
            if (_mPromptDevicePropertyPage != null)
            {
                _mPromptDevicePropertyPage(Handle);
            }
        }

        #region Properties

        public DeviceDescriptor SelectedDevice
        {
            get
            {
                DeviceDescriptor selected = null;
                if (cmbOtherDevices.SelectedIndex >= 0)
                {
                    selected = cmbOtherDevices.Items[cmbOtherDevices.SelectedIndex] as DeviceDescriptor;
                }
                return selected;
            }
        }

        public DeviceCapability SelectedCapability
        {
            get
            {
                DeviceCapability selected = null;
                if (cmbCapabilities.SelectedIndex >= 0)
                {
                    selected = cmbCapabilities.Items[cmbCapabilities.SelectedIndex] as DeviceCapability;
                }
                return selected;
            }
        }

        public string SelectedUrl
        {
            get { return cmbUrl.Text; }
        }

        public NetworkCameraFormat SelectedFormat
        {
            get
            {
                var selected = NetworkCameraFormat.Jpeg;
                if (cmbStreamType.SelectedIndex >= 0)
                {
                    selected = (NetworkCameraFormat)cmbStreamType.SelectedIndex;
                }
                return selected;
            }
        }

        #endregion Properties

        #region Members

        private readonly DeviceDescriptor _mCurrentDevice;
        private readonly PropertyPagePrompter _mPromptDevicePropertyPage;

        #endregion Members
    }
}