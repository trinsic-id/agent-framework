using System;
using System.Collections.Generic;
using System.Text;
using AgentFramework.Core.Messages.EphemeralChallenge;

namespace AgentFramework.Core.Models.EphemeralChallenge
{
    public class CreateChallengeResult
    {
        public string ChallengeId { get; set; }

        public EphemeralChallengeMessage Challenge { get; set; }
    }
}
