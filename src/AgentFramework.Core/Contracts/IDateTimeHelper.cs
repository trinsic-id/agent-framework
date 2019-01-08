using System;

namespace AgentFramework.Core.Contracts
{
    public interface IDateTimeHelper
    {
        DateTime Now();

        DateTime UtcNow();
    }
}
