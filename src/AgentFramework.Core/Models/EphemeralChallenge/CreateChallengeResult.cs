using System;
using System.Collections.Generic;
using System.Text;
using AgentFramework.Core.Messages.EphemeralChallenge;

namespace AgentFramework.Core.Models.EphemeralChallenge
{
    /// <summary>
    /// Create challenge result.
    /// </summary>
    public class CreateChallengeResult
    {
        /// <summary>
        /// Gets or sets the challenge identifier.
        /// </summary>
        /// <value>The challenge identifier.</value>
        public string ChallengeId { get; set; }

        /// <summary>
        /// Gets or sets the challenge.
        /// </summary>
        /// <value>The challenge.</value>
        public EphemeralChallengeMessage Challenge { get; set; }
    }
}
