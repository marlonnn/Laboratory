using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Laboratory.Device;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace Laboratory
{
    [Activity(Label = "FindDeviceActivity")]
    public class FindDeviceActivity : Activity
    {
        private DeviceManager deviceManager;
        private ListView deviceListView;
        private DeviceAdapter deviceAdapter;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            UserDialogs.Init(this);
            deviceManager = DeviceManager.GetDeviceManager();
            deviceManager.DeviceDiscovered += DeviceDiscovered;
            // Create your application here
            SetContentView(Resource.Layout.activity_find_device);
            var scanButton = FindViewById<Button>(Resource.Id.device_scan);
            scanButton.Click += ScanButton_Click;

            deviceListView = FindViewById<ListView>(Resource.Id.deivce_listview);
            deviceAdapter = new DeviceAdapter(this, deviceManager.Devices);
            deviceListView.Adapter = deviceAdapter;
            //deviceAdapter.NotifyDataSetChanged;
        }

        private void DeviceDiscovered(object sender, EventArgs e)
        {
            deviceAdapter.NotifyDataSetChanged();
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            deviceManager.TryStartScanning(true);
        }

        private void ScanDevice()
        {

        }
    }
}