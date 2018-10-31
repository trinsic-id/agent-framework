using System;
using System.Collections.Generic;
using System.Text;
using Streetcred.Sdk.Extensions.Runtime;

namespace Streetcred.Sdk.Extensions
{
    public static class ServiceBuilderExtensions
    {
        public static void AddMemoryCacheLedgerService(this ServicesBuilder builder) => builder.AddExtendedLedgerService<MemoryCacheLedgerService>();
    }
}
