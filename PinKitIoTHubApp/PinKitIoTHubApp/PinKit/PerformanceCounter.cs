using System;
using Microsoft.SPOT;

namespace PinKit
{
    public class PerformanceCounter
    {
        public PerformanceCounter()
        {
            count = 0;
            total = 0;
        }

        public void ShowStatistics()
        {
            Debug.Print("Statistics:");
            Debug.Print("  Count:" + count);
            Debug.Print("  Avg:  "+GetPerformance()+" msec");
        }
        public void StartTF()
        {
            if (status != 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            tfStartTick=DateTime.Now.Ticks;
            status = 1;
        }
        public void EndTF()
        {
            if (status != 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            tfEndTick = DateTime.Now.Ticks;
            status = 0;
            total += (tfEndTick - tfStartTick);
            count++;
        }

        public double GetPerformance()
        {
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return ((double)total / (double)count) / (double)TimeSpan.TicksPerMillisecond;
        }
        public void Clear()
        {
            status = 0;
            count = 0;
            total = 0;
        }
        private int count;
        private double total;

        private long tfStartTick;
        private long tfEndTick;

        private int status = 0;// 0:idle->1:started
    }
}
