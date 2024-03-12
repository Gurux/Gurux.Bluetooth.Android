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
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Lang;

namespace Gurux.Bluetooth
{
    /// <summary>
    /// Properties fragment.
    /// </summary>
    public class GXPropertiesFragment : AndroidX.Fragment.App.Fragment
    {
        private GXPropertiesBase _base;

        /// <inheritdoc/>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var bluetooth = GXPropertiesBase.GetBluetooth();
            View view = inflater.Inflate(Resource.Layout.fragment_properties, container, false);
            ListView properties = (ListView)view.FindViewById(Resource.Id.properties);
            ListView scannedDevices = (ListView)view.FindViewById(Resource.Id.scannedDevices);
            _base = new GXPropertiesBase(properties, scannedDevices, Activity);
            //Stop bluetooth scan button.
            Button stopBtn = view.FindViewById<Button>(Resource.Id.stop);
            //Show Bluetooth scan button.
            Button scanBtn = view.FindViewById<Button>(Resource.Id.scan);
            //Show Bluetooth info.
            Button infoBtn = view.FindViewById<Button>(Resource.Id.showInfo);
            stopBtn.Click += (sender, e) =>
            {
                try
                {
                    stopBtn.Visibility = ViewStates.Gone;
                    scanBtn.Visibility = ViewStates.Visible;
                    infoBtn.Visibility = ViewStates.Visible;
                    scannedDevices.Visibility = ViewStates.Gone;
                    properties.Visibility = ViewStates.Visible;
                    bluetooth.StopScan();
                }
                catch (Exception ex)
                {
                    try
                    {
                        new AlertDialog.Builder(infoBtn.RootView.Context)
                                .SetTitle("Exception")
                                .SetMessage(ex.Message)
                                .SetPositiveButton(Resource.String.ok, (senderAlert, args) => { })
                                .Show();
                    }
                    catch (Exception)
                    {
                    }
                }
            };
            scanBtn.Click += (sender, e) =>
            {
                try
                {
                    bluetooth.Scan();
                    stopBtn.Visibility = ViewStates.Visible;
                    scanBtn.Visibility = ViewStates.Gone;
                    infoBtn.Visibility = ViewStates.Gone;
                    scannedDevices.Visibility = ViewStates.Visible;
                    properties.Visibility = ViewStates.Gone;
                }
                catch (Exception ex)
                {
                    try
                    {
                        new AlertDialog.Builder(infoBtn.RootView.Context)
                                .SetTitle("Exception")
                                .SetMessage(ex.Message)
                                .SetPositiveButton(Resource.String.ok, (senderAlert, args) => { })
                                .Show();
                    }
                    catch (Exception)
                    {
                    }
                }
            };
            infoBtn.Click += (sender, e) =>
            {
                try
                {
                    new AlertDialog.Builder(infoBtn.RootView.Context)
                            .SetTitle("Info")
                            .SetMessage(GXPropertiesBase.GetBluetooth().GetInfo())
                            .SetPositiveButton(Resource.String.ok, (senderAlert, args) => { })
                            .Show();
                }
                catch (Exception)
                {
                }
            };         
            return view;
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            if (_base != null)
            {
                _base.Close();
            }
            base.OnDestroy();
        }
    }
}
