See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.


Join the Gurux Community or follow [@Gurux](https://twitter.com/guruxorg "@Gurux") for project updates.

Open Source GXBluetooth media component, made by Gurux Ltd, is a part of GXMedias set of media components, which programming interfaces help you implement communication by chosen connection type. Our media components also support the following connection types: serial.

For more info check out [Gurux](https://www.gurux.fi/ "Gurux").

We are updating documentation on Gurux web page. 

If you have problems you can ask your questions in Gurux [Forum](https://www.gurux.fi/forum).

Build
=========================== 
If you want to build example you need Nuget package manager for Visual Studio.
You can get it here:
https://visualstudiogallery.msdn.microsoft.com/27077b70-9dad-4c64-adcf-c7cf6bc9970c

Simple example
=========================== 
Gurux Bluetooth component doesn't search available bluetooth devices. It will only use bonded devices. You can bond the Bluetooth device from the Android Bluetooth settings.

Before use you must set following settings:
* Device

It is also good to listen following events:
* OnError
* OnReceived
* OnMediaStateChange
* OnPortAdd
* OnPortRemove

```csharp

GXBluetooth bluetooth = new GXBluetooth(this);
bluetooth.Device = bluetooth .GetDevices()[0].Name;
bluetooth.Open();

```

Data is send with send command:

```csharp
bluetooth.Send("Hello World!");
```
It's good to listen possible errors and show them.

```csharp
bluetooth.OnError += (sender, ex) =>
{
   // Show error.
};
```

In default mode received data is coming as asynchronously from OnReceived event.

```csharp
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

```
Data can be send as syncronous if needed:

```csharp
lock (bluetooth.Synchronous)
{
    string reply = "";
    ReceiveParameters<string> p = new ReceiveParameters<string>()
    //ReceiveParameters<byte[]> p = new ReceiveParameters<byte[]>()
    {
       //Wait time tells how long data is waited.
       WaitTime = 1000,
       //Eop tells End Of Packet charachter.
       Eop = '\r'
    };
    bluetooth.Send("Hello World!", null);
    if (bluetooth.Receive(p))
    {
	reply = Convert.ToString(p.Reply);
    }
}
```