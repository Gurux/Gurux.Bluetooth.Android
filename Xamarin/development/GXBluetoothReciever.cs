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
using Android.Content;
using Android.OS;
using Android.Util;
using System;
using System.Text;
using static Android.Bluetooth.BluetoothClass;

namespace Gurux.Bluetooth
{
    internal class GXBluetoothReciever : BroadcastReceiver
    {
        private readonly GXBluetooth _bluetooth;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bluetooth">Bluetooth owner.</param>
        public GXBluetoothReciever(GXBluetooth bluetooth)
        {
            _bluetooth = bluetooth;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                if (BluetoothDevice.ActionFound == intent.Action)
                {
                    // GetParcelableExtra throws exception in Xamaring. Don't use it.
                    // BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice,
                    // Java.Lang.Class.FromType(typeof(BluetoothDevice)));
                    GXBluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    if (device.BondState == Bond.Bonded)
                    {
                        device = _bluetooth.FindDevice(device._device);
                    }
                    //Add only devices that are not already paired.
                    short value = intent.GetShortExtra(BluetoothDevice.ExtraRssi, short.MinValue);
                    bool changed = device.Rssi != value;
                    if (changed)
                    {
                        device.Rssi = value;
                    }
                    if (device.BondState != Bond.Bonded)
                    {
                        if (_bluetooth.SearchDevice == null ||
                            _bluetooth.SearchDevice.Name == device.Name)
                        {
                            _bluetooth.AddDevice(device);
                            if (_bluetooth.SearchDevice != null)
                            {
                                _bluetooth.SetDevice(device);
                                _bluetooth.SearchDevice = null;
                                Log.Error("Gurux.Bluetooth", "The searched device has been found.");
                                _bluetooth.InitializeDevice();
                            }
                        }
                    }
                    if (changed)
                    {
                        if (_bluetooth._OnBluetoothRssi != null)
                        {
                            _bluetooth._OnBluetoothRssi.Invoke(_bluetooth, device);
                        }
                    }
                }
                else if (BluetoothDevice.ActionPairingRequest == intent.Action)
                {
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    int pin = intent.GetIntExtra(BluetoothDevice.ExtraPairingKey, 1234);
                    device.SetPin(ASCIIEncoding.UTF8.GetBytes(pin.ToString()));
                }
                else if (BluetoothDevice.ActionNameChanged == intent.Action)
                {
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                }
                else if (BluetoothDevice.ExtraRssi == intent.Action)
                {
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    short value = intent.GetShortExtra(BluetoothDevice.ExtraRssi, short.MinValue);
                    foreach (var it in _bluetooth.GetDevices())
                    {
                        if (it.Name == device.Name)
                        {
                            if (_bluetooth._device.Rssi != value)
                            {
                                _bluetooth._device.Rssi = value;
                                if (_bluetooth._OnBluetoothRssi != null)
                                {
                                    _bluetooth._OnBluetoothRssi.Invoke(_bluetooth, it);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Gurux.Bluetooth", ex.Message);
                _bluetooth.NotifyError(ex);
            }
        }
    }
}
