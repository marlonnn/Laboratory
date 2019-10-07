using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Laboratory.Device
{
    public class DeviceAdapter : BaseAdapter<DeviceListViewItem>, IAdapter
    {
        public List<DeviceListViewItem> Devices { get; private set; }
        private Context context;
        public override DeviceListViewItem this[int position] => Devices[position];

        public override int Count => Devices.Count;

        public DeviceAdapter (Context context)
        {
            Devices = new List<DeviceListViewItem>();
            this.context = context;
        }

        public DeviceAdapter(Context context, List<DeviceListViewItem> devices)
        {
            this.context = context;
            this.Devices = devices;
            
        }
        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View row = convertView;
            try
            {
                if (row == null)
                {
                    row = LayoutInflater.From(context).Inflate(Resource.Layout.device_list_item, null, false);
                }
                TextView txtName = row.FindViewById<TextView>(Resource.Id.device_name);
                txtName.Text = Devices[position].Device.Name;

                TextView txtid = row.FindViewById<TextView>(Resource.Id.device_id);
                txtid.Text = Devices[position].Device.Id.ToString("D");
                TextView txtRssi = row.FindViewById<TextView>(Resource.Id.device_rssi);
                txtRssi.Text = Devices[position].Device.Rssi.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally { }
            return row;
        }
    }
}