******************************
Configuration and provisioning
******************************

Available Services
==================

- IProvisioningService
- IConnectionService
- ICredentialService
- IProofService
- IWalletRecordService
- ISchemaService

Dependency injection
====================

When using ASP.NET Core, you can use the extension methods to configure the agent. This will add all required dependencies to the service provider.
If using other tool, you will have to add each required service or message handler manually.

Example if using Autofac

.. code-block:: csharp

    var builder = new ContainerBuilder();

    // Register all required services
    builder.RegisterAssemblyTypes(typeof(IProvisioningService).Assembly)
        .Where(x => x.Namespace.StartsWith("AgentFramework.Core.Runtime", StringComparison.InvariantCulture))
        .AsImplementedInterfaces()
        .SingleInstance();

    // If using message handler package, you can add all handlers
    builder.RegisterAssemblyTypes(typeof(IMessageHandler).Assembly)
        .Where(x => x.IsClass && x is IMessageHandler)
        .AsSelf()
        .SingleInstance();

Provisioning an Agent
=====================

The process of provisioning agents will create and configure an agent wallet.

.. code-block:: csharp

    await _provisioningService.ProvisionAgentAsync(
            new ProvisioningConfiguration
            {
                EndpointUri = "http://localhost:5000",
                OwnerName = "My Agent"
            });

Check the `ProvisioningConfiguration.cs
<https://github.com/streetcred-id/agent-framework/blob/master/src/AgentFramework.Core/Models/Wallets/ProvisioningConfiguration.cs>`_ for full configuration details. You can retrieve the generated details like agent Did and Verkey using

.. code-block:: csharp

    var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

***************
Agent Workflows
***************

Before you begin reading any of the topics below, please familiarize youself with the core idea behind Hyperledger Indy.
We suggest that you go over the `Indy SDK Getting Started Guide
<https://github.com/hyperledger/indy-sdk/blob/master/doc/getting-started/getting-started.md>`_.

Roles and players
=================

Establishing secure connection
==============================

Sending invitations
-------------------

Negotating connection
---------------------

Credential issuence
===================

Issuing credential
------------------

Storing issued credential
-------------------------

Revocation
----------

Proof verification
==================

Proof requests
--------------

Preparing proof
---------------

Verification
------------