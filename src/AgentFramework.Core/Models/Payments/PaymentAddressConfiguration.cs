namespace AgentFramework.Core.Models.Payments
{
    /// <summary>
    /// Payment address configuration.
    /// </summary>
    public class PaymentAddressConfiguration
    {
        /// <summary>
        /// Gets or sets the address seed.
        /// </summary>
        /// <value>The address seed.</value>
        public string AddressSeed { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>The method.</value>
        public string Method { get; set; }
    }
}