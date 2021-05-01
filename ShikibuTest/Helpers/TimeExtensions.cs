using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwallowNest.Shikibu.Tests.Helpers
{
    static class TimeExtensions
    {
        public static TimeSpan Abs(this TimeSpan timeSpan) =>
            timeSpan > TimeSpan.Zero ? timeSpan : -timeSpan;

        public static void WithIn(this TimeSpan timeSpan, TimeSpan delta, string message = "")
            => timeSpan.Abs().Is(abs => abs < delta, message);
    }
}
