using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ploeh.AutoFixture.NUnit3
{
    internal static class TestAdapterHelper
    {
        private const string Discovery = "discovery";

        internal static bool IsDiscovery { get; private set; }

        static TestAdapterHelper()
        {
            try
            {
                var processName = Process.GetCurrentProcess().ProcessName;
                IsDiscovery = CultureInfo.InvariantCulture.CompareInfo.IndexOf(processName, Discovery) != -1;
            }
            catch
            {
            }
        }
    }
}
