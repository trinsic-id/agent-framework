using System;
using AgentFramework.Core.Contracts;

namespace AgentFramework.Core.Helpers
{
    public class DateTimeHelper : IDateTimeHelper
    {
        public DateTime Now() => DateTime.Now;

        public DateTime UtcNow() => DateTime.UtcNow;
    }
}
