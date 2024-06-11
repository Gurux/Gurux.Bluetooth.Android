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

namespace Gurux.Bluetooth
{
    /// <summary>
    /// Bluetooth device is used to save device information 
    /// when device is used without pairing.
    /// </summary>
    public class GXBluetoothDevice
    {
        private string _name;
        internal BluetoothDevice _device;

        /// <summary>
        /// Convert GXBluetoothDevice to BluetoothDevice.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator BluetoothDevice(GXBluetoothDevice value)
        {
            return value._device;
        }

        /// <summary>
        /// Convert BluetoothDevice to GXBluetoothDevice.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator GXBluetoothDevice(BluetoothDevice value)
        {
            return new GXBluetoothDevice()
            {
                _device = value
            };
        }

        /// <summary>
        /// Get the friendly Bluetooth name of the remote device.
        /// </summary>
        public string Name
        {
            get
            {
                if (_device != null)
                {
                    return _device.Name;
                }
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// Received Signal Strength Indication.
        /// </summary>
        public short Rssi
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the friendly Bluetooth name of the remote device.
        /// </summary>
        public string Alias
        {
            get
            {
                if (_device == null)
                {
                    return null;
                }
                return _device.Alias;
            }
        }

        /// <summary>
        /// Returns the hardware address of this BluetoothDevice.
        /// </summary>
        public string Address
        {
            get
            {
                if (_device == null)
                {
                    return null;
                }
                return _device.Address;
            }
        }

        /// <summary>
        /// Returns the supported features (UUIDs) of the remote device.
        /// </summary>
        public ParcelUuid[] GetUuids()
        {
            if (_device == null)
            {
                return null;
            }
            return _device.GetUuids();
        }

        /// <summary>
        /// Perform a service discovery on the remote device to get the UUIDs supported.
        /// </summary>
        public void FetchUuidsWithSdp()
        {
            if (_device != null)
            {
                _device.FetchUuidsWithSdp();
            }
        }

        /// <summary>
        /// Get the bond state of the remote device.
        /// </summary>
        public Bond BondState
        {
            get
            {
                if (_device == null)
                {
                    return Bond.None;
                }
                return _device.BondState;
            }
        }

        /// <summary>
        /// Battery info.
        /// </summary>
        public GXBatteryInfo BatteryInfo
        {
            get;
            private set;
        } = new GXBatteryInfo();
    }
}