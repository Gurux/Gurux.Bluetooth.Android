//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Android.Bluetooth;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Lang;

namespace Gurux.Bluetooth
{
    /// <summary>
    /// Bluetooth port properties.
    /// </summary>
    [Android.App.Activity(Name = "Gurux.Bluetooth.GXProperties")]
    public class GXProperties : AppCompatActivity
    {
        private GXPropertiesBase _base;
        /// <inheritdoc />
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_properties);
            ListView properties = (ListView)FindViewById(Resource.Id.properties);
            ListView scannedDevices = (ListView)FindViewById(Resource.Id.scannedDevices);
            _base = new GXPropertiesBase(properties, scannedDevices, this);
            //Stop bluetooth scan button.
            Button stopBtn = FindViewById<Button>(Resource.Id.stop);
            //Show Bluetooth scan button.
            Button scanBtn = FindViewById<Button>(Resource.Id.scan);

            //Show serial port info.
            Button infoBtn = FindViewById<Button>(Resource.Id.showInfo);

            stopBtn.Click += (sender, e) =>
            {
                try
                {
                    stopBtn.Visibility = ViewStates.Gone;
                    infoBtn.Visibility = ViewStates.Visible;
                    scanBtn.Visibility = ViewStates.Visible;
                    scannedDevices.Visibility = ViewStates.Gone;
                    properties.Visibility = ViewStates.Visible;
                    GXPropertiesBase.GetBluetooth().StopScan();
                }
                catch (Exception)
                {
                }
            };
            scanBtn.Click += (sender, e) =>
            {
                try
                {
                    GXPropertiesBase.GetBluetooth().Scan();
                    stopBtn.Visibility = ViewStates.Visible;
                    scanBtn.Visibility = ViewStates.Gone;
                    infoBtn.Visibility = ViewStates.Gone;
                    scannedDevices.Visibility = ViewStates.Visible;
                    properties.Visibility = ViewStates.Gone;
                }
                catch (Exception)
                {
                }
            };
            infoBtn.Click += (sender, e) =>
            {
                try
                {
                    GXBluetoothDevice device = GXPropertiesBase.GetBluetooth().GetDevice();
                    string info = "";
                    if (device != null)
                    {
                        info = GXBluetooth.GetInfo(device);
                    }
                    new AlertDialog.Builder(infoBtn.RootView.Context)
                            .SetTitle("Info")
                            .SetMessage(info)
                            .SetPositiveButton(Resource.String.ok, (senderAlert, args) => { })
                            .Show();
                }
                catch (Exception)
                {
                }
            };
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            if (_base != null)
            {
                _base.Close();
            }
            base.OnDestroy();
        }
    }
}
