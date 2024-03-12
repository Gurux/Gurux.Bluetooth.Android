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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gurux.Common;
using Gurux.Shared;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using Android.Content;
using Android.App;
using System.Linq;
using Gurux.Bluetooth.Enums;
using System.Threading.Tasks;
using Android.Bluetooth;
using Java.Util;
using System.Text;
using Android.Content.PM;
using AndroidX.Core.App;
using Gurux.Common.Enums;
using Android.Locations;
using Android.Hardware.Usb;
using static Android.Bluetooth.BluetoothClass;
using Android.Companion;

[assembly: UsesFeature("android.hardware.bluetooth")]
[assembly: UsesPermission("android.permission.BLUETOOTH")]
[assembly: UsesPermission("android.permission.BLUETOOTH_CONNECT")]
[assembly: UsesPermission("android.permission.BLUETOOTH_ADMIN")]
[assembly: UsesPermission("android.permission.BLUETOOTH_SCAN")]
[assembly: UsesPermission("android.permission.ACCESS_FINE_LOCATION")]
[assembly: UsesPermission("android.permission.ACCESS_COARSE_LOCATION")]

namespace Gurux.Bluetooth
{
    /// <summary>
    /// Called when the when new bluetooth device is found.
    /// </summary>
    /// <param name="device">Added bluetooth deviec.</param>
    public delegate void DeviceAddEventHandler(BluetoothDevice device);

    /// <summary>
    /// Called when the when bluetooth device is removed.
    /// </summary>
    /// <param name="device">Removed bluetooth device.</param>
    public delegate void DeviceRemoveEventHandler(BluetoothDevice device);

    /// <summary>
    /// A media component that enables communication of bluetooth for Android devices.
    /// See help in https://www.gurux.fi/Gurux.Bluetooth
    /// </summary>
    public class GXBluetooth : IGXMedia2, IDisposable
    {

        Task _receiver = null;

        /// <summary>
        /// Is bluetooth closing.
        /// </summary>
        public ManualResetEvent _closing = new ManualResetEvent(false);
        /// <summary>
        /// Is bluetooth received closed.
        /// </summary>
        public ManualResetEvent _closed = new ManualResetEvent(false);

        BluetoothDevice _device;
        private Context _contect;
        private DeviceAddEventHandler _OnDeviceAdd;
        private DeviceRemoveEventHandler _OnDeviceRemove;

        private object m_sync = new object();
        int LastEopPos = 0;
        TraceLevel m_Trace;
        object m_Eop;
        private readonly GXSynchronousMediaBase _syncBase;
        UInt64 _bytesSent, m_BytesReceived;
        readonly object m_Synchronous = new object();
        BluetoothSocket _socket;
        private readonly GXBluetoothReciever _Receiver;
        /// <summary>
        /// Not binded devices.
        /// </summary>
        private readonly List<BluetoothDevice> _devices = new List<BluetoothDevice>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXBluetooth(Context contect)
        {
            _contect = contect;
            _Receiver = new GXBluetoothReciever(this);
            IntentFilter filter = new IntentFilter();
            filter.AddAction(BluetoothDevice.ActionFound);           
            contect.RegisterReceiver(_Receiver, new IntentFilter(filter));
            _syncBase = new GXSynchronousMediaBase(1024);
            BluetoothManager manager = (BluetoothManager)contect.GetSystemService(Context.BluetoothService);
            if (manager.Adapter == null)
            {
                throw new Exception("Bluetooth is not available.");
            }
            //Enable Bluetooth adapter.
            if (!manager.Adapter.IsEnabled)
            {
                if (contect is Activity activity)
                {
                    activity.StartActivityForResult(new Intent(BluetoothAdapter.ActionRequestEnable), 1);
                }
            }
            LocationManager locationManager = (LocationManager)contect.GetSystemService(Context.LocationService);
            if (!locationManager.IsProviderEnabled(LocationManager.GpsProvider))
            {
                if (contect is Activity activity)
                {
                    activity.StartActivityForResult(new Intent(LocationManager.ExtraLocationEnabled), 1);
                }
            }
            GetDevices();
        }

        private bool CheckAccessRights()
        {
            if (_contect is Activity activity)
            {
                List<string> missing = new List<string>();
                if (Permission.Denied == _contect.CheckSelfPermission("android.permission.BLUETOOTH"))
                {
                    missing.Add("android.permission.BLUETOOTH");
                }
                if (Permission.Denied == _contect.CheckSelfPermission("android.permission.BLUETOOTH_CONNECT"))
                {
                    missing.Add("android.permission.BLUETOOTH_CONNECT");
                }
                if (Permission.Denied == _contect.CheckSelfPermission("android.permission.BLUETOOTH_SCAN"))
                {
                    missing.Add("android.permission.BLUETOOTH_SCAN");
                }
                if (Permission.Denied == _contect.CheckSelfPermission("android.permission.ACCESS_COARSE_LOCATION"))
                {
                    missing.Add("android.permission.ACCESS_COARSE_LOCATION");
                }
                if (Permission.Denied == _contect.CheckSelfPermission("android.permission.ACCESS_FINE_LOCATION"))
                {
                    missing.Add("android.permission.ACCESS_FINE_LOCATION");
                }
                if (missing.Any())
                {
                    ActivityCompat.RequestPermissions(activity, missing.ToArray(), 2);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// New Bluetooth device is found.
        /// </summary>
        public event DeviceAddEventHandler OnDeviceAdd
        {
            add
            {
                _OnDeviceAdd += value;
            }
            remove
            {
                _OnDeviceAdd -= value;
            }
        }

        /// <summary>
        /// Bluetooth device has been removed.
        /// </summary>
        public event DeviceRemoveEventHandler OnDeviceRemove
        {
            add
            {
                _OnDeviceRemove += value;
            }
            remove
            {
                _OnDeviceRemove -= value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="activity">Activity.</param>
        public GXBluetooth(Activity activity) : this((Context)activity)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="device">Bluetooth device name.</param>
        public GXBluetooth(Context context, string device) : this(context)
        {
            foreach (BluetoothDevice it in GetDevices())
            {
                if (string.Compare(device, it.Name, true) == 0)
                {
                    _device = it;
                    break;
                }
            }
        }

        internal void AddDevice(BluetoothDevice device)
        {
            //Check that device doesn't exist.
            foreach (var it in GetDevices())
            {
                if (it.Address == device.Address)
                {
                    return;
                }
            }
            _devices.Add(device);
            _OnDeviceAdd?.Invoke(device);
        }

        internal void NotifyError(System.Exception ex)
        {
            if (m_OnError != null)
            {
                m_OnError(this, ex);
            }
            if (m_Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex, null));
            }
        }

        void NotifyMediaStateChange(MediaState state)
        {
            if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, state, null));
            }
            if (m_OnMediaStateChange != null)
            {
                m_OnMediaStateChange(this, new MediaStateEventArgs(state));
            }
        }

        /// <summary>
        /// What level of tracing is used.
        /// </summary>
        public TraceLevel Trace
        {
            get
            {
                return m_Trace;
            }
            set
            {
                m_Trace = _syncBase.Trace = value;
            }
        }

        private void HandleReceivedData(int index, byte[] buffer, int totalCount)
        {
            lock (_syncBase.receivedSync)
            {
                if (totalCount != 0 && Eop != null) //Search Eop if given.
                {
                    byte[] eop = null;
                    if (Eop is Array)
                    {
                        foreach (object it in (Array)Eop)
                        {
                            eop = Gurux.Common.GXCommon.GetAsByteArray(it);
                            totalCount = Gurux.Common.GXCommon.IndexOf(_syncBase.m_Received, eop, index, _syncBase.receivedSize);
                            if (totalCount != -1)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        eop = Common.GXCommon.GetAsByteArray(Eop);
                        totalCount = Common.GXCommon.IndexOf(_syncBase.m_Received, eop, index, _syncBase.receivedSize);
                    }
                    if (totalCount != -1)
                    {
                        totalCount += eop.Length;
                    }
                }
            }
            if (totalCount != -1 && m_Trace == TraceLevel.Verbose && m_OnTrace != null)
            {
                int pos;
                //If sync data is not read.
                if (index + totalCount >= LastEopPos)
                {
                    pos = LastEopPos;
                }
                else //If sync data is read.
                {
                    pos = index;
                }
                int count;
                if (totalCount > LastEopPos)
                {
                    count = totalCount - LastEopPos;
                }
                else
                {
                    count = totalCount;
                }
                TraceEventArgs arg = new TraceEventArgs(TraceTypes.Received,
                                    buffer, 0, count, null);
                LastEopPos = index + totalCount;
                if (LastEopPos != 0)
                {
                    --LastEopPos;
                }
                m_OnTrace(this, arg);
            }
            if (this.IsSynchronous)
            {
                if (totalCount != -1)
                {
                    _syncBase.AppendData(buffer, index, totalCount);
                    _syncBase.receivedEvent.Set();
                }
            }
            else if (this.m_OnReceived != null)
            {
                if (totalCount != -1)
                {
                    byte[] buff = new byte[totalCount];
                    Array.Copy(buffer, buff, totalCount);
                    m_OnReceived(this, new ReceiveEventArgs(buff, Device));
                }
            }
        }

        /// <summary>
        /// Is bluetooth connection established.
        /// </summary>
        [Browsable(false)]
        public bool IsOpen
        {
            get
            {
                return _socket != null && _socket.IsConnected;
            }
        }

        /// <summary>
        /// Gets or sets the Bluetooth device.
        /// </summary>
        [Browsable(true)]
        public string Device
        {
            get
            {
                return _device?.Name;
            }
            set
            {
                bool change = true;
                if (_device != null)
                {
                    change = value != _device.Name;
                }
                if (change)
                {
                    _device = null;
                    foreach (BluetoothDevice it in GetDevices())
                    {
                        if (it.Name == value)
                        {
                            _device = it;
                            break;
                        }
                    }
                    NotifyPropertyChanged("Device");
                }
            }
        }

        /// <summary>
        /// Gets or sets the Bluetooth device.
        /// </summary>
        public BluetoothDevice GetDevice()
        {
            return _device;
        }

        /// <summary>
        /// Start scanning.
        /// </summary>
        /// <returns></returns>
        public void Scan()
        {
            BluetoothManager manager = (BluetoothManager)_contect.GetSystemService(Context.BluetoothService);
            if (manager.Adapter.IsDiscovering)
            {
                manager.Adapter.CancelDiscovery();
            }
            if (!manager.Adapter.StartDiscovery())
            {
                throw new Exception("Failed to start discovery.");
            }
        }

        /// <summary>
        /// Stop scanning.
        /// </summary>
        /// <returns></returns>
        public void StopScan()
        {
            BluetoothManager manager = (BluetoothManager)_contect.GetSystemService(Context.BluetoothService);
            if (manager.Adapter.IsDiscovering)
            {
                manager.Adapter.CancelDiscovery();
            }
        }

        /// <summary>
        /// Returns bluetooth device information.
        /// </summary>
        /// <returns></returns>
        public string GetInfo()
        {
            BluetoothDevice device = GetDevice();
            StringBuilder sb = new StringBuilder();
            if (device != null)
            {
                if (!string.IsNullOrEmpty(device.Name))
                {
                    sb.Append("Name: ");
                    sb.AppendLine(device.Name);
                }
                if (!string.IsNullOrEmpty(device.Alias))
                {
                    sb.Append("Alias: ");
                    sb.AppendLine(device.Alias);
                }
                sb.Append("Addres: ");
                sb.AppendLine(device.Address);
                sb.AppendLine("");
                sb.AppendLine("UUIDs: ");
                var list = device.GetUuids();
                if (list != null)
                {
                    foreach (var it in list)
                    {
                        sb.AppendLine(it.Uuid.ToString());
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets or sets the Bluetooth device.
        /// </summary>
        internal void SetDevice(BluetoothDevice value)
        {
            _device = value;
        }

        /// <summary>
        /// Closes the Bluetooth connection.
        /// </summary>
        public void Close()
        {
            if (_receiver != null && !_receiver.IsCompleted)
            {
                _closing.Set();
                _closed.WaitOne(1000);
                _closed.Reset();
                _receiver = null;
            }
            if (_socket != null)
            {
                try
                {
                    NotifyMediaStateChange(MediaState.Closing);
                    _socket.Close();
                }
                finally
                {
                    NotifyMediaStateChange(MediaState.Closed);
                    _socket = null;
                }
            }
        }

        /// <summary>
        /// Gets an array of availble bluetooth devices.
        /// </summary>
        /// <returns></returns>
        public BluetoothDevice[] GetDevices()
        {
            if (CheckAccessRights())
            {
                return new BluetoothDevice[0];
            }
            List<BluetoothDevice> devices = new List<BluetoothDevice>();
            BluetoothManager manager = (BluetoothManager)_contect.GetSystemService(Context.BluetoothService);
            var list = manager.GetConnectedDevices(ProfileType.Gatt);
            devices.AddRange(list);
            list = manager.GetConnectedDevices(ProfileType.GattServer);
            devices.AddRange(list);
            devices.AddRange(manager.Adapter.BondedDevices);
            //Add scanned devices.
            devices.AddRange(_devices);
            //manager.Adapter.GetRemoteLeDevice
            return devices.ToArray();
        }

        /// <summary>
        /// Opens a new bluetooth connection.
        /// </summary>
        public void Open()
        {
            Close();
            try
            {
                _closing.Reset();
                _closed.Reset();
                if (_device == null)
                {
                    throw new System.Exception("Bluetooth device is not selected.");
                }
                NotifyMediaStateChange(MediaState.Opening);
                if (Trace >= TraceLevel.Info)
                {
                    string eopString = "None";
                    if (m_Eop is byte[] b)
                    {
                        eopString = Common.GXCommon.ToHex(b);
                    }
                    else if (m_Eop != null)
                    {
                        eopString = m_Eop.ToString();
                    }
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Info,
                            "Settings: Device: " + Device, null));
                }
                //bluetooth.
                UUID Ssp = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
                BluetoothManager manager = (BluetoothManager)_contect.GetSystemService(Context.BluetoothService);
                if (manager.Adapter == null)
                {
                    throw new Exception("Bluetooth is not available.");
                }
                manager.Adapter.CancelDiscovery();
                if (_device.BondState == Bond.None)
                {
                    _socket = _device.CreateInsecureRfcommSocketToServiceRecord(Ssp);
                }
                else
                {
                    _socket = _device.CreateRfcommSocketToServiceRecord(Ssp);
                }
                _socket.Connect();
                _receiver = Task.Run(() =>
                {
                    byte[] tmp = new byte[1024];
                    while (!_closing.WaitOne(0))
                    {
                        try
                        {
                            int count = _socket.InputStream.Read(tmp, 0, tmp.Length);
                            if (count != 0)
                            {
                                HandleReceivedData(0, tmp, count);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!_closing.WaitOne(0))
                            {
                                NotifyError(ex);
                            }
                        }
                    }
                    _closed.Set();
                });
                NotifyMediaStateChange(MediaState.Open);
            }
            catch (Exception ex)
            {
                Close();
                throw;
            }
        }

        #region Events
        /// <summary>
        /// GXNet component sends received data through this method.
        /// </summary>
        [Description("GXNet component sends received data through this method.")]
        public event ReceivedEventHandler OnReceived
        {
            add
            {
                m_OnReceived += value;
            }
            remove
            {
                m_OnReceived -= value;
            }
        }

        /// <summary>
        /// Errors that occur after the connection is established, are sent through this method.
        /// </summary>
        [Description("Errors that occur after the connection is established, are sent through this method.")]
        public event Gurux.Common.ErrorEventHandler OnError
        {
            add
            {

                m_OnError += value;
            }
            remove
            {
                m_OnError -= value;
            }
        }

        /// <summary>
        /// Media component sends notification, when its state changes.
        /// </summary>
        [Description("Media component sends notification, when its state changes.")]
        public event MediaStateChangeEventHandler OnMediaStateChange
        {
            add
            {
                m_OnMediaStateChange += value;
            }
            remove
            {
                m_OnMediaStateChange -= value;
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                m_OnPropertyChanged += value;
            }
            remove
            {
                m_OnPropertyChanged -= value;
            }
        }

        /// <inheritdoc cref="TraceEventHandler"/>
        [Description("Called when the Media is sending or receiving data.")]
        public event TraceEventHandler OnTrace
        {
            add
            {
                m_OnTrace += value;
            }
            remove
            {
                m_OnTrace -= value;
            }
        }

        private void NotifyPropertyChanged(string info)
        {
            if (m_OnPropertyChanged != null)
            {
                m_OnPropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }


        //Events
        TraceEventHandler m_OnTrace;
        PropertyChangedEventHandler m_OnPropertyChanged;
        MediaStateChangeEventHandler m_OnMediaStateChange;
        internal Common.ErrorEventHandler m_OnError;
        ReceivedEventHandler m_OnReceived;

        /// <inheritdoc />
        public event ClientConnectedEventHandler OnClientConnected;
        /// <inheritdoc />
        public event ClientDisconnectedEventHandler OnClientDisconnected;
        #endregion //Events

        /// <inheritdoc />
        public AvailableMediaSettings ConfigurableSettings
        {
            get
            {
                return (AvailableMediaSettings)((IGXMedia)this).ConfigurableSettings;
            }
            set
            {
                ((IGXMedia)this).ConfigurableSettings = (int)value;
            }
        }

        /// <inheritdoc cref="IGXMedia.Tag"/>
        public object Tag
        {
            get;
            set;
        }

        /// <inheritdoc />
        [Browsable(false), ReadOnly(true)]
        public object SyncRoot
        {
            get
            {
                //In some special cases when binary serialization is used this might be null
                //after deserialize. Just set it.
                if (m_sync == null)
                {
                    m_sync = new object();
                }
                return m_sync;
            }
        }

        /// <inheritdoc />
        public object Synchronous
        {
            get
            {
                return m_Synchronous;
            }
        }

        /// <inheritdoc />
        public bool IsSynchronous
        {
            get
            {
                bool reserved = System.Threading.Monitor.TryEnter(m_Synchronous, 0);
                if (reserved)
                {
                    System.Threading.Monitor.Exit(m_Synchronous);
                }
                return !reserved;
            }
        }

        /// <inheritdoc />
        public void ResetSynchronousBuffer()
        {
            lock (_syncBase.receivedSync)
            {
                _syncBase.receivedSize = 0;
            }
        }

        #region IGXMedia Members

        /// <summary>
        /// Sent byte count.
        /// </summary>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesSent
        {
            get
            {
                return _bytesSent;
            }
        }

        /// <summary>
        /// Received byte count.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesReceived
        {
            get
            {
                return m_BytesReceived;
            }
        }

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        public void ResetByteCounters()
        {
            _bytesSent = m_BytesReceived = 0;
        }

        /// <inheritdoc />
        void Gurux.Common.IGXMedia.Copy(object target)
        {
            GXBluetooth Target = (GXBluetooth)target;
            Device = Target.Device;
        }

        /// <inheritdoc />
        public object Eop
        {
            get
            {
                return m_Eop;
            }
            set
            {
                bool change = m_Eop != value;
                m_Eop = value;
                if (change)
                {
                    NotifyPropertyChanged("Eop");
                }
            }
        }

        /// <summary>
        /// Media settings as a XML string.
        /// </summary>
        public string Settings
        {
            get
            {
                string tmp = "";
                if (Device != null && !string.IsNullOrEmpty(Device))
                {
                    tmp += "<Device>" + Device + "</Device>";
                }
                return tmp;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Device":
                                        {
                                            Device = xmlReader.ReadString();
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Current Bluetooth port settings as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (_device != null)
            {
                sb.Append(_device.Name);
                sb.Append(' ');
            }
            return sb.ToString();
        }

        /// <inheritdoc />
        string IGXMedia.MediaType
        {
            get
            {
                return "Bluetooth";
            }
        }

        /// <inheritdoc />
        bool IGXMedia.Enabled
        {
            get
            {
                return GetDevices().Any();
            }
        }

        /// <inheritdoc />
        string IGXMedia.Name
        {
            get
            {
                return _device?.Name;
            }
        }

        /// <summary>
        /// Shows the Bluetooth Properties dialog.
        /// </summary>
        /// <param name="activity">Owner window of the Properties dialog.</param>
        /// <returns>True, if the user has accepted the changes.</returns>
        /// <seealso cref="Device">Device</seealso>
        public bool Properties(Activity activity)
        {
            GXPropertiesBase.SetBluetooth(this);
            Intent intent = new Intent(activity, typeof(GXProperties));
            activity.StartActivity(intent);
            return true;
        }

        /// <summary>
        /// Returns a new instance of the Settings form.
        /// </summary>
        public AndroidX.Fragment.App.Fragment PropertiesForm
        {
            get
            {
                GXPropertiesBase.SetBluetooth(this);
                return new GXPropertiesFragment();
            }
        }

        /// <summary>
        /// Sends data asynchronously. <br/>
        /// No reply from the receiver, whether or not the operation was successful, is expected.
        /// </summary>
        public void Send(object data)
        {
            ((Gurux.Common.IGXMedia)this).Send(data, null);
        }

        /// <inheritdoc />
        public bool Receive<T>(Gurux.Common.ReceiveParameters<T> args)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Media is closed.");
            }
            return _syncBase.Receive(args);
        }

        /// <inheritdoc />
        void IGXMedia.Send(object data, string receiver)
        {
            byte[] buff = Gurux.Common.GXCommon.GetAsByteArray(data);
            if (buff == null)
            {
                throw new System.Exception("Data send failed. Invalid data.");
            }
            if (Trace == TraceLevel.Verbose)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, data, null));
            }
            if (buff.Any())
            {
                _socket.OutputStream.Write(buff, 0, buff.Length);
                _bytesSent += (UInt64)buff.Length;
            }
        }

        /// <inheritdoc />
        public void Validate()
        {

        }

        /// <inheritdoc />
        int Gurux.Common.IGXMedia.ConfigurableSettings
        {
            get;
            set;
        }

        /// <inheritdoc />
        uint IGXMedia2.AsyncWaitTime
        {
            get;
            set;
        }

        /// <inheritdoc />
        EventWaitHandle IGXMedia2.AsyncWaitHandle
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc />
        public uint ReceiveDelay
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Dispose()
        {
            if (IsOpen)
            {
                Close();
            }
            if (_contect != null)
            {
                _contect.UnregisterReceiver(_Receiver);
                _contect = null;
            }
        }

        #endregion
    }
}
