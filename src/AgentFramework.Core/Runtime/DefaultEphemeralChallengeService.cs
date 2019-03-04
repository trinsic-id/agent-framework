using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.EphemeralChallenge;
using AgentFramework.Core.Models.EphemeralChallenge;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultEphemeralChallengeService : IEphemeralChallengeService
    {
        /// <summary>
        /// The event aggregator.
        /// </summary>
        protected readonly IEventAggregator EventAggregator;
        /// <summary>
        /// The proof service.
        /// </summary>
        protected readonly IProofService ProofService;
        /// <summary>
        /// The record service.
        /// </summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<DefaultEphemeralChallengeService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEphemeralChallengeService"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="proofService">The proof service.</param>
        /// <param name="recordService">The record service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultEphemeralChallengeService(
            IEventAggregator eventAggregator,
            IProofService proofService,
            IWalletRecordService recordService,
            ILogger<DefaultEphemeralChallengeService> logger)
        {
            EventAggregator = eventAggregator;
            ProofService = proofService;
            RecordService = recordService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateChallengeConfigAsync(IAgentContext agentContext, EphemeralChallengeConfiguration config)
        {
            EphemeralChallengeConfigRecord configRecord = new EphemeralChallengeConfigRecord
            {
                Id = Guid.NewGuid().ToString(),
                Name = config.Name,
                Type = config.Type,
                Contents = config.Contents
            };

            await RecordService.AddAsync(agentContext.Wallet, configRecord);
            return configRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<EphemeralChallengeConfigRecord> GetChallengeConfigAsync(IAgentContext agentContext, string configId)
        {
            Logger.LogInformation(LoggingEvents.GetChallengeConfiguration, "Configuration Id {0}", configId);

            var record = await RecordService.GetAsync<EphemeralChallengeConfigRecord>(agentContext.Wallet, configId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Challenge configuration record not found");

            return record;
        }

        /// <inheritdoc />
        public virtual Task<List<EphemeralChallengeConfigRecord>> ListChallengeConfigsAsync(IAgentContext agentContext, ISearchQuery query = null,
            int count = 100)
        {
            Logger.LogInformation(LoggingEvents.ListChallengeConfigurations, "List Challenge Configurations");

            return RecordService.SearchAsync<EphemeralChallengeConfigRecord>(agentContext.Wallet, query, null, count);
        }

        /// <inheritdoc />
        public virtual async Task<EphemeralChallengeRecord> GetChallengeAsync(IAgentContext agentContext, string challengeId)
        {
            Logger.LogInformation(LoggingEvents.GetChallengeConfiguration, "Configuration Id {0}", challengeId);

            var record = await RecordService.GetAsync<EphemeralChallengeRecord>(agentContext.Wallet, challengeId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Challenge record not found");

            return record;
        }

        /// <inheritdoc />
        public virtual Task<List<EphemeralChallengeRecord>> ListChallengesAsync(IAgentContext agentContext, ISearchQuery query = null,
            int count = 100)
        {
            Logger.LogInformation(LoggingEvents.ListChallengeConfigurations, "List Challenges");

            return RecordService.SearchAsync<EphemeralChallengeRecord>(agentContext.Wallet, query, null, count);
        }

        /// <inheritdoc />
        public virtual async Task<CreateChallengeResult> CreateChallengeAsync(IAgentContext agentContext,
            string challengeConfigId)
        {
            var config = await GetChallengeConfigAsync(agentContext, challengeConfigId);
            EphemeralChallengeRecord challengeRecord = new EphemeralChallengeRecord
            {
                Id = Guid.NewGuid().ToString()
            };
            EphemeralChallengeMessage challengeMessage = new EphemeralChallengeMessage();

            if (config.Type == ChallengeType.Proof)
            {
                var proofRequestConfig = (ProofRequestConfiguration)config.Contents;
                (var proofRequest, var _) = await ProofService.CreateProofRequestAsync(agentContext, new ProofRequest
                {
                    Name = config.Name,
                    Version = "1.0",
                    Nonce = Guid.NewGuid().ToString(),
                    RequestedAttributes = proofRequestConfig.RequestedAttributes,
                    RequestedPredicates = proofRequestConfig.RequestedPredicates,
                    NonRevoked = proofRequestConfig.NonRevoked
                });
                challengeRecord.Challenge = new EphemeralChallengeContents
                {
                    Type = ChallengeType.Proof,
                    Contents = JsonConvert.DeserializeObject<ProofRequest>(proofRequest.ProofRequestJson)
                };
                challengeMessage.Challenge = challengeRecord.Challenge;
            }

            var message = new EphemeralChallengeMessage
            {
                Challenge = challengeRecord.Challenge,
            };

            challengeRecord.Tags.Add(TagConstants.Role, TagConstants.Requestor);
            challengeRecord.Tags.Add(TagConstants.LastThreadId, message.Id);
            await RecordService.AddAsync(agentContext.Wallet, challengeRecord);

            return new CreateChallengeResult
            {
                ChallengeId = challengeRecord.Id,
                Challenge = message
            };
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessChallengeAsync(IAgentContext agentContext,
            EphemeralChallengeMessage challenge)
        {
            EphemeralChallengeRecord challengeRecord = new EphemeralChallengeRecord
            {
                Id = Guid.NewGuid().ToString(),
                Challenge = challenge.Challenge
            };

            challengeRecord.Tags.Add(TagConstants.Role, TagConstants.Holder);
            await RecordService.AddAsync(agentContext.Wallet, challengeRecord);
            return challengeRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessChallengeResponseAsync(IAgentContext agentContext,
            EphemeralChallengeResponseMessage challengeResponse)
        {
            var threadId = challengeResponse.GetThreadId();

            var results = await ListChallengesAsync(agentContext, new EqSubquery(TagConstants.LastThreadId, threadId));

            var record = results.First();

            record.Response = challengeResponse.Response;

            if (challengeResponse.Status == EphemeralChallengeResponseStatus.Accepted)
                await record.TriggerAsync(ChallengeTrigger.AcceptChallenge);
            else
                await record.TriggerAsync(ChallengeTrigger.RejectChallenge);

            await RecordService.AddAsync(agentContext.Wallet, record);
            return record.Id;
        }

        /// <inheritdoc />
        public virtual async Task<EphemeralChallengeResponseMessage> AcceptChallenge(IAgentContext agentContext, EphemeralChallengeMessage message)
        {
            var recordId = await ProcessChallengeAsync(agentContext, message);
            var challengeRecord = await GetChallengeAsync(agentContext, recordId);
            var challengeResponse = new EphemeralChallengeResponseMessage
            {
                Id = Guid.NewGuid().ToString(),
                Status = EphemeralChallengeResponseStatus.Accepted
            };


            if (challengeRecord.Challenge.Type == ChallengeType.Proof)
            {
                var proofRequest = (ProofRequest) challengeRecord.Challenge.Contents;

                var proof = await ProofService.CreateProofAsync(agentContext, proofRequest, new RequestedCredentials());
                challengeResponse.Response = new EphemeralChallengeContents
                {
                    Type = ChallengeType.Proof,
                    Contents = proof
                };
            }

            return challengeResponse;
        }
    }
}
