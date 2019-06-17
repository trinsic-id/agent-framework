using System;
using AgentFramework.Core.Models.Payments;

namespace AgentFramework.Payments.SovrinToken
{
    public class SovrinPaymentAccountConfiguration : PaymentAccountConfiguration
    {
        /// <summary>
        /// Gets or sets the address seed.
        /// </summary>
        /// <value>The address seed.</value>
        public string AddressSeed { get; set; }
    }
}
