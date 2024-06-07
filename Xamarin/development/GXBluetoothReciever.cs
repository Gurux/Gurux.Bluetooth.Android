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
using Android.Nfc;
using Android.Util;
using System;
using System.Text;

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
                    // GetParcelableExtra throws expeption in Xamaring. Don't use it.
                    // BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice,
                    // Java.Lang.Class.FromType(typeof(BluetoothDevice)));
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    //Add only devices that are not already paired.
                    if (device.BondState != Bond.Bonded)
                    {
                        _bluetooth.AddDevice(device);
                    }
                }
                else if (BluetoothDevice.ActionPairingRequest == intent.Action)
                {
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    int pin = intent.GetIntExtra(BluetoothDevice.ExtraPairingKey, 1234);
                    device.SetPin(ASCIIEncoding.UTF8.GetBytes(pin.ToString()));
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
