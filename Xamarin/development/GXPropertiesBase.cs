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

using Android.App;
using Android.Content;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System;
using Android.Bluetooth;
using Android.Views;

namespace Gurux.Bluetooth
{
    internal class GXPropertiesBase
    {
        private readonly Context activity;
        private readonly ListView _properties;
        private readonly ListView _scannedDevices;
        private List<string> rows = new List<string>();
        private List<string> _devices = new List<string>();
        private static GXBluetooth _bluetooth;

        public GXPropertiesBase(ListView properties, ListView scannedDevices, Context c)
        {
            scannedDevices.Visibility = ViewStates.Gone;
            var bluetooth = GXPropertiesBase.GetBluetooth();
            bluetooth.OnDeviceAdd += Bluetooth_OnDeviceAdd;
            activity = c;
            _properties = properties;
            _scannedDevices = scannedDevices;
            //Select first device if it's not selected.
            if (_bluetooth.GetDevice() == null)
            {
                BluetoothDevice[] devices = _bluetooth.GetDevices();
                if (devices.Any())
                {
                    _bluetooth.SetDevice(devices[0]);
                }
            }
            rows.Add(GetDevice());
            _devices = _bluetooth.GetDevices().Select(s => s.Name).ToList();

            //Add Bluetooth settings.
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                    Resource.Layout.support_simple_spinner_dropdown_item, rows);
            _properties.Adapter = adapter;
            _properties.ItemClick += (sender, e) =>
            {
                switch (e.Position)
                {
                    case 0:
                        UpdateDevice();
                        break;
                    default:
                        //Do nothing.
                        break;
                };
            };

            //Add scanned bluetooth devices.
            adapter = new ArrayAdapter<string>(activity,
                    Resource.Layout.support_simple_spinner_dropdown_item, _devices);
            _scannedDevices.Adapter = adapter;          
        }

        private void Bluetooth_OnDeviceAdd(BluetoothDevice device)
        {
            _devices.Add(device.Name);
            //Add scanned bluetooth devices.
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                    Resource.Layout.support_simple_spinner_dropdown_item, _devices);
            _scannedDevices.Adapter = adapter;
        }

        public void Close()
        {
            var bluetooth = GetBluetooth();
            if (bluetooth != null)
            {
                bluetooth.OnDeviceAdd -= Bluetooth_OnDeviceAdd;
            }
        }

        public static GXBluetooth GetBluetooth()
        {
            return _bluetooth;
        }

        public static void SetBluetooth(GXBluetooth value)
        {
            _bluetooth = value;
        }

        private string GetDevice()
        {
            if (_bluetooth.GetDevice() == null)
            {
                return "";
            }
            return activity.GetString(Resource.String.device) + "\r\n" + _bluetooth.GetDevice().Name;
        }

        /// <summary>
        /// Update bluetooth device.
        /// </summary>
        private void UpdateDevice()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            List<string> values = new List<string>();
            foreach (var it in _bluetooth.GetDevices())
            {
                values.Add(it.Name);
            }
            string actual = _bluetooth.Device;
            int selected = -1;
            int pos = 0;
            foreach (string it in values)
            {
                //Find selected item.
                if (actual == it)
                {
                    selected = pos;
                    break;
                }
                ++pos;
            }
            if (values.Any())
            {
                builder.SetTitle(Resource.String.device)
                        .SetSingleChoiceItems(values.ToArray(), selected, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
                {
                    _bluetooth.Device = values[e.Which];
                    rows[0] = GetDevice();
                    ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                    Resource.Layout.support_simple_spinner_dropdown_item, rows);
                    _properties.Adapter = adapter;
                    var d = (sender as AlertDialog);
                    d.Dismiss();
                }
            )).Show();
            }
            else
            {
                _bluetooth.Device = null;
                rows[0] = GetDevice();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                Resource.Layout.support_simple_spinner_dropdown_item, rows);
                _properties.Adapter = adapter;
            }
        }
    }
}
