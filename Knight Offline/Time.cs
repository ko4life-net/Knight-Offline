using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Knight_Offline
{
    // Necessary informations about QPC can be obtained here https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps

    public class Time
    {
        static private bool IsStopwatchMethod;
        static private long Frequency, StartingTime, CurrentTime, PreviousTime;
        public Dictionary<string, Interval> Intervals; // = new Dictionary<string, Interval>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryPerformanceFrequency(out long Frequency);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool QueryPerformanceCounter(out long PerformanceCount);

        /*
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        struct LARGE_INTEGER
        {
            [FieldOffset(0)]
            public uint LowPart;
            [FieldOffset(4)]
            public int HighPart;
            [FieldOffset(0)]
            public long QuadPart;
        }
        */

        public Time()
        {
            if (GetType().Name == "Time")
            {
                Intervals = new Dictionary<string, Interval>();

                if (Stopwatch.IsHighResolution)
                {
                    IsStopwatchMethod = true;
                    Frequency = Stopwatch.Frequency;
                    StartingTime = Stopwatch.GetTimestamp();
                }
                // QueryPerformanceFrequency() method
                else
                {
                    IsStopwatchMethod = false;

                    // If we cannot use QueryPerformanceFrequency() then we should implement our own mechanism as it is in the game client
                    if (QueryPerformanceFrequency(out Frequency))
                    {
                        if (!QueryPerformanceCounter(out StartingTime))
                        {
                            Debug.WriteLine("QueryPerformanceCounter() failed");

                            throw new Win32Exception();
                        }
                    }
                    else
                    {
                        Debug.WriteLine("QueryPerformanceFrequency() failed");

                        throw new Win32Exception();
                    }
                }
            }
        }

        public void UpdateTime()
        {
            PreviousTime = CurrentTime;

            // The computer clock is usually accurate to ± 1 second per day
            if (IsStopwatchMethod)
            {
                CurrentTime = Stopwatch.GetTimestamp();
            }
            else
            {
                if (!QueryPerformanceCounter(out CurrentTime))
                {
                    Debug.WriteLine("QueryPerformanceCounter() failed");

                    throw new Win32Exception();
                }
            }

            if (Intervals.Count > 0)
            {
                float ElapsedTime = GetElapsedTime();

                foreach(var Interval in Intervals.Values)
                {
                    Interval.IntervalTime += ElapsedTime;
                }
            }
        }

        private float GetElapsedTime()
        {
            // Casting is necessary, don't follow Visual Studio's hints!
            return (float)((double)(CurrentTime - PreviousTime) / (double)Frequency);
        }

        public float GetTotalElapsedTime()
        {
            // Casting is necessary, don't follow Visual Studio's hints!
            return (float)((double)(CurrentTime - StartingTime) / (double)Frequency);
        }
    }

    public class Interval : Time
    {
        public float IntervalTime;

        public Interval() : base()
        {

        }

        public bool IsTimeElapsed(float Condition)
        {
            if (IntervalTime > Condition)
            {
                IntervalTime = 0;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}