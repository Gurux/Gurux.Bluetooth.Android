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
using Android.Runtime;
using Android.Util;
using Gurux.Common.Enums;
using Java.Util;
using System;
using System.Linq;
using static Android.Bluetooth.BluetoothClass;

namespace Gurux.Bluetooth
{
    internal class GXBluetoothGattCallback : BluetoothGattCallback
    {
        private static UUID BatteryServiceUuid = UUID.FromString("0000180F-0000-1000-8000-00805f9b34fb");
        private static UUID BatteryLevelCharacteristicUuid = UUID.FromString("00002A19-0000-1000-8000-00805f9b34fb");
        private static UUID BatteryPowerStateCharacteristicUuid = UUID.FromString("00002a1a-0000-1000-8000-00805f9b34fb");
        private readonly GXBluetooth _bluetooth;
        private readonly Context _context;
        internal BluetoothGattCharacteristic ReadCharacteristic;
        internal BluetoothGattCharacteristic WriteCharacteristic;
        internal BluetoothGattDescriptor ReadDescriptor;
        internal BluetoothGattCharacteristic BatteryCharacteristic;
        
        /// <summary>
        /// Contructor.
        /// </summary>
        internal GXBluetoothGattCallback(Context context, GXBluetooth bluetooth)
        {
            _context = context;
            _bluetooth = bluetooth;
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt,
            BluetoothGattCharacteristic characteristic,
            byte[] value,
            [GeneratedEnum] GattStatus status)
        {
            if (BatteryCharacteristic != null && characteristic.Uuid == BatteryCharacteristic.Uuid &&
                _bluetooth._device != null)
            {
                _bluetooth._device.BatteryInfo.Capacity = value[0];
                if (_bluetooth._OnBatteryCapacity != null)
                {
                    _bluetooth._OnBatteryCapacity.Invoke(_bluetooth, _bluetooth._device);
                }
            }
            base.OnCharacteristicRead(gatt, characteristic, value, status);
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt,
            BluetoothGattCharacteristic characteristic, byte[] value)
        {
            base.OnCharacteristicChanged(gatt, characteristic, value);
            if (ReadCharacteristic != null &&
                characteristic.Uuid == ReadCharacteristic.Uuid &&
                value != null && value.Any())
            {
                _bluetooth.HandleReceivedData(0, value, value.Length);
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, [GeneratedEnum] GattStatus status)
        {
            BluetoothGattService service = gatt.GetService(BatteryServiceUuid);
            if (service != null)
            {
                BatteryCharacteristic = service.GetCharacteristic(BatteryLevelCharacteristicUuid);
                if (BatteryCharacteristic != null)
                {
                    if (BatteryCharacteristic.Properties.HasFlag(GattProperty.Notify))
                    {
                        gatt.SetCharacteristicNotification(BatteryCharacteristic, true);
                    }
                    gatt.ReadCharacteristic(BatteryCharacteristic);
                }
            }
            base.OnServicesDiscovered(gatt, status);
            //Read manufacturer spesific UUID from file.
            using (var reader = new System.IO.StreamReader(_context.Assets.Open("devices.csv")))
            {
                foreach (var row in reader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!row.StartsWith("#"))
                    {
                        var cells = row.Split(';');
                        if (cells.Length != 4)
                        {
                            throw new ArgumentException("Invalid device. " + row);
                        }
                        if (string.Compare(cells[0], _bluetooth._device.Name, true) == 0)
                        {
                            UUID uuid = UUID.FromString(cells[1]);
                            service = gatt.GetService(uuid);
                            if (service == null)
                            {
                                throw new Exception("Unknown service. " + uuid);
                            }
                            //Get read characteristic.
                            uuid = UUID.FromString(cells[2]);
                            ReadCharacteristic = service.GetCharacteristic(uuid);
                            if (ReadCharacteristic == null)
                            {
                                throw new Exception("Unknown read characteristic. " + uuid);
                            }
                            if (ReadCharacteristic.Properties.HasFlag(GattProperty.Notify))
                            {
                                gatt.SetCharacteristicNotification(ReadCharacteristic, true);
                            }
                            //Get write characteristic.
                            uuid = UUID.FromString(cells[3]);
                            WriteCharacteristic = service.GetCharacteristic(uuid);
                            if (WriteCharacteristic == null ||
                                !WriteCharacteristic.Properties.HasFlag(GattProperty.Write))
                            {
                                throw new Exception("Unknown write characteristic. " + uuid);
                            }
                            break;
                        }
                    }
                }
            }
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt,
            [GeneratedEnum] GattStatus status,
            [GeneratedEnum] ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);
            if (newState == ProfileState.Connected)
            {
                _bluetooth.NotifyMediaStateChange(MediaState.Open);
                if (!gatt.Services.Any())
                {
                    gatt.DiscoverServices();
                }
            }
            else if (newState == ProfileState.Disconnected)
            {
                _bluetooth.Close();
            }
        }
    }
}
