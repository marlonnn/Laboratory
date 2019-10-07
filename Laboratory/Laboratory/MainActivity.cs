using System;
using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Laboratory.Device;
using Microcharts.Droid;
using Laboratory.Data;
using Microcharts;
using System.Threading;
using SkiaSharp;

namespace Laboratory
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private DeviceManager deviceManager;
        private DataManager dataManager;
        private ListView deviceListView;
        private DeviceAdapter deviceAdapter;
        private ChartView chartView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            //Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            //SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            UserDialogs.Init(this);
            deviceManager = DeviceManager.GetDeviceManager();
            deviceManager.DeviceDiscovered += DeviceDiscovered;

            dataManager = DataManager.GetDataManager();
            dataManager.UpdateChartHandler += UpdateChart;
            LineChart lineChart = new LineChart() { Entries = dataManager.DayData };
            
            chartView = FindViewById<ChartView>(Resource.Id.chartView);
            chartView.Chart = lineChart;
            //chartView.Chart = new 
            // Create your application here
            //SetContentView(Resource.Layout.activity_find_device);
            //var scanButton = FindViewById<Button>(Resource.Id.btnScan);
            //scanButton.Click += ScanButton_Click;

            //deviceListView = FindViewById<ListView>(Resource.Id.devicesListView);
            //deviceAdapter = new DeviceAdapter(this, deviceManager.Devices);
            //deviceListView.Adapter = deviceAdapter;
            //deviceListView.ItemClick += DeviceListView_ItemClick;

            //var notifyButton = FindViewById<Button>(Resource.Id.btnNotify);
            //notifyButton.Click += NotifyButton_Click;

        }

        private void UpdateChart(object sender, EventArgs e)
        {
            chartView.Invalidate();
        }
       

        private bool notifyEnable = false;
        private void NotifyButton_Click(object sender, EventArgs e)
        {
            if (notifyEnable)
            {
                deviceManager.TryStopCharacteristicNotifications();
                notifyEnable = false;
            }
            else
            {
                deviceManager.TryStartCharacteristicNotifications();
                notifyEnable = true;
            }
        }

        private void DeviceListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            DeviceListViewItem deviceListView = deviceManager.Devices[e.Position];
            deviceManager.TryConnectDevice(deviceListView.Device);

        }

        protected override void OnResume()
        {
            dataManager.ResumeThread();
            base.OnResume();
        }

        protected override void OnStop()
        {
            dataManager.StopThread();
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            dataManager.DestroyThread();
            base.OnDestroy();
            //deviceManager.DisconnectDevice();
        }
        private void DeviceDiscovered(object sender, EventArgs e)
        {
            deviceAdapter.NotifyDataSetChanged();
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            deviceManager.TryStartScanning(true);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }
            if (id == Resource.Id.bluetooth_settings)
            {
                var findDeviceActivity = new Intent(this, typeof(FindDeviceActivity));
                StartActivity(findDeviceActivity);
            }
            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

