using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Extensions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using IAdapter = Plugin.BLE.Abstractions.Contracts.IAdapter;

namespace Laboratory.Device
{
    public class DeviceManager
    {
        private static DeviceManager deviceManager;
        private IBluetoothLE bluetoothLe;
        public IBluetoothLE BluetoothLE
        {
            get { return this.bluetoothLe; }
            set { this.bluetoothLe = value; }
        }
        private IAdapter adapter;
        public IAdapter Adapter
        {
            get { return this.adapter; }
            set { this.adapter = value; }
        }
        public bool IsRefreshing => adapter.IsScanning;
        public bool IsStateOn => bluetoothLe.IsOn;
        public string StateText => GetStateText();

        private CancellationTokenSource cancellationTokenSource;

        private List<DeviceListViewItem> devices;
        public List<DeviceListViewItem> Devices
        {
            get { return this.devices; }
            set { this.devices = value; }
        }

        private IUserDialogs userDialogs;
        public IUserDialogs IUserDialogs
        {
            get { return userDialogs; }
            set { this.userDialogs = value; }
        }

        public EventHandler DeviceDiscovered;

        private IPermissions permissions;

        public IReadOnlyList<IService> Services { get; private set; }

        private IDevice connectedDevice;
        public IDevice ConnectedDeice
        {
            get { return this.connectedDevice; }
            set { this.connectedDevice = value; }
        }

        private ICharacteristic characteristic;

        public DeviceManager()
        {
            bluetoothLe = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;

            devices = new List<DeviceListViewItem>();

            userDialogs = UserDialogs.Instance;
            bluetoothLe.StateChanged += OnStateChanged;
            Adapter.DeviceDiscovered += OnDeviceDiscovered;
            Adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;
            Adapter.DeviceDisconnected += OnDeviceDisconnected;
            Adapter.DeviceConnectionLost += OnDeviceConnectionLost;
            permissions = CrossPermissions.Current;

        }

        public void CharacteristicOnValueUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs)
        {
            var bytes = characteristicUpdatedEventArgs.Characteristic.Value;
            Plugin.BLE.Abstractions.Trace.Message(bytes.ToString());
        }

        public async void TryStartCharacteristicNotifications()
        {
            if (ConnectedDeice != null)
            {
                try
                {
                    Guid guid = ConnectedDeice.Id;
                    var services = await ConnectedDeice.GetServicesAsync();
                    foreach (var s in services)
                    {
                        Console.WriteLine("Id: " + s.Id + "Name: " + s.Name);
                    }
                    var service = await ConnectedDeice.GetServiceAsync(Guid.Parse("00000000-0000-0000-0000-246f284c556a"));
                    var service2 = await ConnectedDeice.GetServiceAsync(Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e"));
                    if (service != null)
                    {
                        var characteristic = await service.GetCharacteristicAsync(guid);
                        if (characteristic != null)
                        {
                            characteristic.ValueUpdated -= CharacteristicOnValueUpdated;
                            characteristic.ValueUpdated += CharacteristicOnValueUpdated;
                            await characteristic.StartUpdatesAsync();
                        }
                    }

                    if (service2 != null)
                    {
                        var rxCharacteristic = await service2.GetCharacteristicAsync(Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e"));
                        var txCharacteristic = await service2.GetCharacteristicAsync(Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e"));
                        byte[] data = { 0, 1, 2 };
                        if (rxCharacteristic != null)
                        {
                            await rxCharacteristic.WriteAsync(data);
                        }
                        if (txCharacteristic != null)
                        {
                            txCharacteristic.ValueUpdated += TxCharacteristic_ValueUpdated;
                            await txCharacteristic.StartUpdatesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void TxCharacteristic_ValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var bytes = e.Characteristic.Value;
            string result = System.Text.Encoding.UTF8.GetString(bytes);
            Plugin.BLE.Abstractions.Trace.Message(result);
            Console.WriteLine("rx data: " + result);
        }

        public async void TryStopCharacteristicNotifications()
        {
            if (characteristic != null)
            {
                await characteristic.StopUpdatesAsync();
            }
            
        }

        public async void TryGetServices(bool showPrompt = false)
        {
            if (ConnectedDeice != null)
            {
                try
                {
                    if (showPrompt) userDialogs.ShowLoading("Discovering services...");
                    Services = await ConnectedDeice.GetServicesAsync();
                }
                catch (Exception ex)
                {
                    await userDialogs.AlertAsync(ex.Message, "Error while discovering services");
                    Plugin.BLE.Abstractions.Trace.Message(ex.Message);
                }
                finally
                {
                    if (showPrompt)  userDialogs.HideLoading();
                }

            }
        }

        public void DisconnectDevice()
        {
            if (ConnectedDeice != null && ConnectedDeice.State == DeviceState.Connected)
            {
                if (Adapter != null) Adapter.DisconnectDeviceAsync(ConnectedDeice);
            }
        }
        public async Task<bool> TryConnectDevice(IDevice device, bool showPrompt = true)
        {
            if (showPrompt && !await userDialogs.ConfirmAsync($"Connect to device '{device.Name}'?"))
            {
                return false;
            }
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                var config = new ProgressDialogConfig()
                {
                    Title = $"Connecting to '{device.Id}'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                using (var progress = userDialogs.Progress(config))
                {
                    progress.Show();

                    await Adapter.ConnectToDeviceAsync(device, new ConnectParameters(autoConnect: false, forceBleTransport: true), tokenSource.Token);
                }
                ConnectedDeice = device;
                await userDialogs.AlertAsync($"Connected to {device.Name}.");
                return true;

            }
            catch (Exception ex)
            {
                await userDialogs.AlertAsync(ex.Message, "Connection error");
                Plugin.BLE.Abstractions.Trace.Message(ex.Message);
                return false;
            }
            finally
            {
                userDialogs.HideLoading();
            }
        }

        public async void TryStartScanning(bool refresh = false)
        {
            try
            {
                var status = await permissions.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted)
                {
                    var permissionResult = await permissions.RequestPermissionsAsync(Permission.Location);

                    if (permissionResult.First().Value != PermissionStatus.Granted)
                    {
                        await userDialogs.AlertAsync("Permission denied. Not scanning.");
                        permissions.OpenAppSettings();
                        return;
                    }
                }
                if (IsStateOn && (refresh || !Devices.Any()) && !IsRefreshing)
                {
                    ScanForDevices();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async void ScanForDevices()
        {
            Devices.Clear();

            foreach (var connectedDevice in Adapter.ConnectedDevices)
            {
                //update rssi for already connected evices (so tha 0 is not shown in the list)
                try
                {
                    await connectedDevice.UpdateRssiAsync();
                }
                catch (Exception ex)
                {
                    Plugin.BLE.Abstractions.Trace.Message(ex.Message);
                    await userDialogs.AlertAsync($"Failed to update RSSI for {connectedDevice.Name}");
                }

                AddOrUpdateDevice(connectedDevice);
            }
            cancellationTokenSource = new CancellationTokenSource();
            Adapter.ScanMode = ScanMode.LowLatency;
            await Adapter.StartScanningForDevicesAsync(cancellationTokenSource.Token);
        }

        private void AddOrUpdateDevice(IDevice newDevice)
        {
            var vm = Devices.FirstOrDefault(d => d.Device.Id == newDevice.Id);
            if (vm != null)
            {
                vm.Device = newDevice;
            }
            else
            {
                Devices.Add(new DeviceListViewItem(newDevice));
            }
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e)
        {
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e)
        {

        }

        private void Adapter_ScanTimeoutElapsed(object sender, EventArgs e)
        {

        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs e)
        {
            AddOrUpdateDevice(e.Device);
            DeviceDiscovered?.Invoke(sender, e);
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e)
        {

        }

        public static DeviceManager GetDeviceManager()
        {
            if (deviceManager == null) deviceManager = new DeviceManager();
            return deviceManager;
        }

        private string GetStateText()
        {
            switch (bluetoothLe.State)
            {
                case BluetoothState.Unknown:
                    return "Unknown BLE state.";
                case BluetoothState.Unavailable:
                    return "BLE is not available on this device.";
                case BluetoothState.Unauthorized:
                    return "You are not allowed to use BLE.";
                case BluetoothState.TurningOn:
                    return "BLE is warming up, please wait.";
                case BluetoothState.On:
                    return "BLE is on.";
                case BluetoothState.TurningOff:
                    return "BLE is turning off. That's sad!";
                case BluetoothState.Off:
                    return "BLE is off. Turn it on!";
                default:
                    return "Unknown BLE state.";
            }
        }
    }
}