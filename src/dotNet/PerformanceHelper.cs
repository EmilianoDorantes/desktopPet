using System;
using System.Diagnostics;

namespace DesktopPet
{
    public class PerformanceHelper : IDisposable
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private bool _available = false;

        public PerformanceHelper()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _cpuCounter.NextValue();
                _available = true;
            }
            catch
            {
                _available = false;
            }
        }

        public bool IsAvailable => _available;

        public float GetCpuUsage()
        {
            if (!_available) return 0;
            try { return _cpuCounter.NextValue(); }
            catch { return 0; }
        }

        public float GetAvailableRamMB()
        {
            if (!_available) return 0;
            try { return _ramCounter.NextValue(); }
            catch { return 0; }
        }

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
        }
    }
}
