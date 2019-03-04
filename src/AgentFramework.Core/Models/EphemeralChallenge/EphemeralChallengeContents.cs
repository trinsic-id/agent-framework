using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Models.EphemeralChallenge
{
    /// <summary>
    /// An ephemeral challenge contents representation.
    /// </summary>
    public class EphemeralChallengeContents
    {
        /// <summary>
        /// Type of the challenge.
        /// </summary>
        public ChallengeType Type { get; set; }

        /// <summary>
        /// Contents of the challenge.
        /// </summary>
        public dynamic Contents { get; set; }
    }
}
