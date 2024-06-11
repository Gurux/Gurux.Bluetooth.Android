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
using Android.OS;

namespace Gurux.Bluetooth
{
    /// <summary>
    /// Bluetooth battery information.
    /// </summary>
    public class GXBatteryInfo
    {
        /// <summary>
        /// Battery capacity.
        /// </summary>
        public int Capacity
        {
            get;
            set;
        }

        /// <summary>
        /// Battery status.
        /// </summary>
        public BatteryStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// Battery charge counter.
        /// </summary>
        public int ChargeCounter
        {
            get;
            set;
        }

        /// <summary>
        /// Battery average current.
        /// </summary>
        public int CurrentAverage
        {
            get;
            set;
        }

        /// <summary>
        /// Battery current now.
        /// </summary>
        public int CurrentNow
        {
            get;
            set;
        }
        /// <summary>
        /// Battery energy counter.
        /// </summary>
        public long EnergyCounter
        {
            get;
            set;
        }
    }
}
