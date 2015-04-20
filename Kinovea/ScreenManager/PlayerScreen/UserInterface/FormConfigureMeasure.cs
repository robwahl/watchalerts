#region License

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

#endregion License

using Kinovea.ScreenManager.Languages;
using log4net;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     This dialog let the user specify how many real-world-units long is this line.
    ///     This will have an impact on all lines stored in this metadata.
    ///     Note that it is not possible to map pixels to pixels.
    ///     Pixel are used exclusively internally.
    /// </summary>
    public partial class FormConfigureMeasure : Form
    {
        #region User choices handlers

        private void tbFPSOriginal_KeyPress(object sender, KeyPressEventArgs e)
        {
            // We only accept numbers, points and coma in there.
            var key = e.KeyChar;
            if (((key < '0') || (key > '9')) && (key != ',') && (key != '.') && (key != '\b'))
            {
                e.Handled = true;
            }
        }

        #endregion User choices handlers

        #region Members

        private readonly Metadata _mMetadata;
        private readonly DrawingLine2D _mLine;
        private readonly double _mFCurrentLengthPixels;

        private readonly double _mFCurrentLengthReal;
        // The current length of the segment. Might be expressed in pixels.

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Construction & Initialization

        public FormConfigureMeasure(Metadata metadata, DrawingLine2D line)
        {
            _mMetadata = metadata;
            _mLine = line;

            _mFCurrentLengthPixels =
                Math.Sqrt(((_mLine.MStartPoint.X - _mLine.MEndPoint.X) * (_mLine.MStartPoint.X - _mLine.MEndPoint.X)) +
                          ((_mLine.MStartPoint.Y - _mLine.MEndPoint.Y) * (_mLine.MStartPoint.Y - _mLine.MEndPoint.Y)));
            _mFCurrentLengthReal = _mMetadata.CalibrationHelper.GetLengthInUserUnit(_mLine.MStartPoint, _mLine.MEndPoint);

            Log.Debug(string.Format("Initial length:{0:0.00} {1}", _mFCurrentLengthReal,
                _mMetadata.CalibrationHelper.CurrentLengthUnit));

            InitializeComponent();
            LocalizeForm();
        }

        private void LocalizeForm()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureMeasure_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            grpConfig.Text = ScreenManagerLang.Generic_Configuration;
            lblRealSize.Text = ScreenManagerLang.dlgConfigureMeasure_lblRealSize.Replace("\\n", "\n");

            // Combo Units (MUST be filled in the order of the enum)
            cbUnit.Items.Add(ScreenManagerLang.LengthUnit_Centimeters + " (" +
                             CalibrationHelper.GetLengthAbbreviationFromUnit(LengthUnits.Centimeters) + ")");
            cbUnit.Items.Add(ScreenManagerLang.LengthUnit_Meters + " (" +
                             CalibrationHelper.GetLengthAbbreviationFromUnit(LengthUnits.Meters) + ")");
            cbUnit.Items.Add(ScreenManagerLang.LengthUnit_Inches + " (" +
                             CalibrationHelper.GetLengthAbbreviationFromUnit(LengthUnits.Inches) + ")");
            cbUnit.Items.Add(ScreenManagerLang.LengthUnit_Feet + " (" +
                             CalibrationHelper.GetLengthAbbreviationFromUnit(LengthUnits.Feet) + ")");
            cbUnit.Items.Add(ScreenManagerLang.LengthUnit_Yards + " (" +
                             CalibrationHelper.GetLengthAbbreviationFromUnit(LengthUnits.Yards) + ")");

            // Update with current values.
            if (_mMetadata.CalibrationHelper.CurrentLengthUnit == LengthUnits.Pixels)
            {
                // Default to 50 cm if no unit selected yet.
                tbMeasure.Text = "50";
                cbUnit.SelectedIndex = (int)LengthUnits.Centimeters;
            }
            else
            {
                tbMeasure.Text = string.Format("{0:0.00}", _mFCurrentLengthReal);
                cbUnit.SelectedIndex = (int)_mMetadata.CalibrationHelper.CurrentLengthUnit;
            }
        }

        #endregion Construction & Initialization

        #region OK/Cancel Handlers

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (tbMeasure.Text.Length > 0)
            {
                // Save value.
                try
                {
                    var fRealWorldMeasure = double.Parse(tbMeasure.Text);

                    if (fRealWorldMeasure > 0 && _mFCurrentLengthReal > 0)
                    {
                        _mMetadata.CalibrationHelper.PixelToUnit = fRealWorldMeasure / _mFCurrentLengthPixels;
                        _mMetadata.CalibrationHelper.CurrentLengthUnit = (LengthUnits)cbUnit.SelectedIndex;

                        Log.Debug(string.Format("Selected length:{0:0.00} {1}", fRealWorldMeasure,
                            _mMetadata.CalibrationHelper.CurrentLengthUnit));
                    }
                }
                catch
                {
                    // Failed : do nothing.
                    Log.Error(string.Format("Error while parsing measure. ({0}).", tbMeasure.Text));
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Nothing more to do.
        }

        #endregion OK/Cancel Handlers
    }
}