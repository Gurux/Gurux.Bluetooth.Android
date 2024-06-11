using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Android.Widget;
using System.Collections.Generic;
using Xamarin.Essentials;
using System.Linq;
using Gurux.Common;
using Gurux.Common.Enums;
using System.Text;
using Android.Bluetooth;
using static Android.Bluetooth.BluetoothClass;

namespace Gurux.Bluetooth.Example
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        List<string> _devices = new List<string>();

        /// <summary>
        /// List of available bluetooth devices.
        /// </summary>
        private Spinner deviceList;
        private Button clearData;
        private Button showInfo;
        private Button openBtn;
        private Button sendBtn;
        private Button propertiesBtn;
        private GXBluetooth bluetooth;
        private TextView receivedData;
        private EditText sendData;
        private CheckBox hex;


        ArrayAdapter<string> deviceAdapter;

        private T GetValue<T>(int id, T def)
        {
            string value = SecureStorage.GetAsync(GetString(id)).Result;
            if (string.IsNullOrEmpty(value))
            {
                return def;
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private void SetValue(int id, object value)
        {
            SecureStorage.SetAsync(GetString(id), Convert.ToString(value)).Wait();
        }


        /// <summary>
        /// Read last used settings.
        /// </summary>
        private void ReadSettings()
        {
            if (bluetooth != null)
            {
                bluetooth.Device = GetValue<string>(Resource.String.device, "");
            }
            hex.Checked = GetValue<bool>(Resource.String.hex, true);
            sendData.Text = GetValue<string>(Resource.String.sendData, "");
        }

        /// <summary>
        /// Save last used settings.
        /// </summary>
        private void SaveSettings()
        {
            SetValue(Resource.String.device, bluetooth.Device);
            SetValue(Resource.String.hex, hex.Checked);
            SetValue(Resource.String.sendData, sendData.Text);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            clearData = FindViewById<Button>(Resource.Id.clearData);
            clearData.Click += (sender, e) =>
            {
                ClearData();
            };
            showInfo = FindViewById<Button>(Resource.Id.showInfo);
            showInfo.Click += (sender, e) =>
            {
                ShowInfo();
            };
            deviceList = FindViewById<Spinner>(Resource.Id.deviceList);
            deviceAdapter = new ArrayAdapter<string>(this,
            Resource.Layout.support_simple_spinner_dropdown_item, _devices);
            deviceAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
            deviceList.Adapter = deviceAdapter;
            deviceList.ItemSelected += (sender, e) =>
            {
                var list = bluetooth.GetDevices();
                if (e.Position < list.Length)
                {
                    bluetooth.Device = list[e.Position].Name;
                }
            };

            openBtn = FindViewById<Button>(Resource.Id.openBtn);
            openBtn.Click += (sender, e) =>
            {
                OpenBluetooth();
            };
            sendBtn = FindViewById<Button>(Resource.Id.sendBtn);
            sendBtn.Click += (sender, e) =>
            {
                SendData();
            };

            propertiesBtn = FindViewById<Button>(Resource.Id.propertiesBtn);
            propertiesBtn.Click += (sender, e) =>
            {
                bluetooth.Properties(this);
            };

            receivedData = FindViewById<TextView>(Resource.Id.receivedData);
            sendData = FindViewById<EditText>(Resource.Id.sendData);
            hex = FindViewById<CheckBox>(Resource.Id.hex);
            try
            {
                //Add bluetooth ports.
                bluetooth = new GXBluetooth(this);
                bluetooth.PropertyChanged += (s, e) =>
                {
                    SaveSettings();
                    //Select new device.
                    if (bluetooth.Device != null)
                    {
                        for (int pos = 0; pos != deviceAdapter.Count; ++pos)
                        {
                            if (deviceAdapter.GetItem(pos) == bluetooth.Device)
                            {
                                deviceList.SetSelection(pos);
                                break;
                            }
                        }
                    }
                };

                bluetooth.OnError += (sender, ex) =>
                {
                    ShowError(ex);
                };
                bluetooth.OnReceived += (sender, e) =>
                {
                    try
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (hex.Checked)
                            {
                                receivedData.Append(GXCommon.ToHex((byte[])e.Data));
                            }
                            else
                            {
                                receivedData.Append(ASCIIEncoding.ASCII.GetString((byte[])e.Data));
                            }
                        });
                    }
                    catch (System.Exception ex)
                    {
                        ShowError(ex);
                    }
                };
                bluetooth.OnMediaStateChange += (sender, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (e.State == MediaState.Open)
                        {
                            sendBtn.Enabled = true;
                            openBtn.Text = GetString(Resource.String.close);
                        }
                        else if (e.State == MediaState.Closed)
                        {
                            sendBtn.Enabled = false;
                            openBtn.Text = GetString(Resource.String.open);
                        }
                    });
                };
                if (!_devices.Any())
                {
                    openBtn.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        private void ShowError(Exception ex)
        {
            Console.WriteLine(ex.Message);
            new Android.App.AlertDialog.Builder(this)
                    .SetTitle("Error")
                    .SetMessage(ex.Message)
                    .SetPositiveButton(GetString(Resource.String.ok), (senderAlert, args) => { })
                .Show();
        }

        /// <summary>
        /// Open selected bluetooth connection.
        /// </summary>
        public void OpenBluetooth()
        {
            try
            {
                string open = GetString(Resource.String.open);
                if (openBtn.Text == open)
                {
                    bluetooth.Open();
                }
                else
                {
                    bluetooth.Close();
                }
            }
            catch (Exception ex)
            {
                bluetooth.Close();
                ShowError(ex);
            }
        }

        /// <summary>
        /// Send data to the bluetooth device.
        /// </summary>
        public void SendData()
        {
            try
            {
                string str = sendData.Text;
                if (hex.Checked)
                {
                    bluetooth.Send(GXCommon.HexToBytes(str));
                }
                else
                {
                    bluetooth.Send(str);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        static T Cast<T>(Java.Lang.Object obj) where T : class
        {
            var propertyInfo = obj.GetType().GetProperty("Instance");
            return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as T;
        }

        /// <summary>
        /// Show bluetooth device info.
        /// </summary>
        public void ShowInfo()
        {
            try
            {
                if (deviceList.SelectedItem != null)
                {
                    new Android.App.AlertDialog.Builder(this)
                            .SetTitle("Info")
                            .SetMessage(bluetooth.GetInfo())
                            .SetPositiveButton(GetString(Resource.String.ok), (senderAlert, args) => { })
                        .Show();
                }
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        /// <summary>
        /// Clear received data.
        /// </summary>
        public void ClearData()
        {
            try
            {
                receivedData.Text = "";
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        /// <inheritdoc />
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        /// <inheritdoc />
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <inheritdoc />
        protected override void OnResume()
        {
            Console.WriteLine("OnResume");
            deviceAdapter.Clear();
            ReadSettings();
            if (bluetooth != null)
            {
                foreach (GXBluetoothDevice it in bluetooth.GetDevices())
                {
                    deviceAdapter.Add(it.Name);
                }
                deviceAdapter.NotifyDataSetChanged();
            }
            openBtn.Enabled = deviceAdapter.Count != 0;
            base.OnResume();
        }

        /// <inheritdoc />
        protected override void OnPause()
        {
            Console.WriteLine("OnPause");
            base.OnPause();
            try
            {
                SaveSettings();
                bluetooth.Close();
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }
    }
}
