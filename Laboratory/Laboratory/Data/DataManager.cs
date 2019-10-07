using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microcharts;
using SkiaSharp;

namespace Laboratory.Data
{
    public class DataManager
    {
        public const int MAXDAYCOUNT = 15;
        private static DataManager dataManager;

        private List<Entry> dayData;
        public List<Entry> DayData
        {
            get { return this.dayData; }
        }
        private List<Entry> weekData;
        private List<Entry> monthData;

        private Thread thread;
        private bool runThread = false;
        public EventHandler UpdateChartHandler;
        private int index = 0;
        public DataManager()
        {
            dayData = new List<Entry>();
            weekData = new List<Entry>();
            monthData = new List<Entry>();
            //SimulatorData();
            StartThread();
        }

        private void StartThread()
        {
            runThread = true;
            thread = new Thread(new ThreadStart(Simulate));
            thread.Start();
        }

        public void StopThread()
        {
            runThread = false;
        }

        public void ResumeThread()
        {
            runThread = true;
        }

        public void DestroyThread()
        {
            runThread = false;
            if (thread != null) thread.Abort();
        }

        public static DataManager GetDataManager()
        {
            if (dataManager == null)
            {
                dataManager = new DataManager();
            }
            return dataManager;
        }

        private void Simulate()
        {
            Random random = new Random(20);
            while (true)
            {
                if (runThread)
                {
                    var r = random.Next(10, 15) / 10f;
                    var value = 20f * r;
                    dataManager.AddEntry(value, string.Format("{0}", index++), ColorManager.HSL2RGB(value / 30, 0.5, 0.5));
                    Thread.Sleep(1000);
                    UpdateChartHandler?.Invoke(null, null);
                }
            }
        }


        public void AddEntry(float value, string label)
        {
            AddEntry(CreateEntry(value, label));
        }

        public void AddEntry(float value, string label, SKColor color)
        {
            AddEntry(CreateEntry(value, label, color));
        }

        private void AddEntry(Entry entry)
        {
            if (dayData.Count > MAXDAYCOUNT)
            {
                dayData.RemoveAt(0);
            }
            dayData.Add(entry);
        }

        private Entry CreateEntry(float value, string label)
        {
            return new Entry(value) { Label = label, ValueLabel = value.ToString()};
        }

        private Entry CreateEntry(float value, string label, SKColor color)
        {
            return new Entry(value) { Label = label, ValueLabel = value.ToString(), Color = color};
        }

        private void SimulatorData()
        {
            dayData.Add(new Entry(20)
            {
                Label = "t1",
                ValueLabel = "20",
                Color = SKColor.Parse("#266489")
            });
            dayData.Add(new Entry(28)
            {
                Label = "t2",
                ValueLabel = "28",
                Color = SKColor.Parse("#68B9C0")
            });
            dayData.Add(new Entry(25)
            {
                Label = "t3",
                ValueLabel = "25",
                Color = SKColor.Parse("#90D585")
            });
            dayData.Add(new Entry(26)
            {
                Label = "t4",
                ValueLabel = "26",
                Color = SKColor.Parse("#266489")
            });
            dayData.Add(new Entry(24)
            {
                Label = "t5",
                ValueLabel = "24",
                Color = SKColor.Parse("#68B9C0")
            });
            dayData.Add(new Entry(27)
            {
                Label = "t6",
                ValueLabel = "27",
                Color = SKColor.Parse("#90D585")
            });
        }
    }
}