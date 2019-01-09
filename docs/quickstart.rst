******************************
Configuration and provisioning
******************************

Services overview
=================

- IProvisioningService
- IConnectionService
- ICredentialService
- IProofService
- IWalletRecordService
- ISchemaService

Dependency injection
====================

When using ASP.NET Core, you can use the extension methods to configure the agent. This will add all required dependencies to the service provider.
Additionaly, the AgentFramework depends on the Logging extensions. These need to be added as well.

If using other tool, you will have to add each required service or message handler manually.

Example if using Autofac

.. code-block:: csharp

    // .NET Core dependency collection
    var services = new ServiceCollection();
    services.AddLogging();

    // Autofac builder
    var builder = new ContainerBuilder();

    // Register all required services
    builder.RegisterAssemblyTypes(typeof(IProvisioningService).Assembly)
        .Where(x => x.Namespace.StartsWith("AgentFramework.Core.Runtime", 
            StringComparison.InvariantCulture))
        .AsImplementedInterfaces()
        .SingleInstance();

    // If using message handler package, you can add all handlers
    builder.RegisterAssemblyTypes(typeof(IMessageHandler).Assembly)
        .Where(x => x.IsClass && x is IMessageHandler)
        .AsSelf()
        .SingleInstance();

    builder.Populate(services);

Check the `Xamarin Sample
<https://github.com/streetcred-id/agent-framework/blob/master/samples/xamarin-forms/AFMobileSample/App.xaml.cs>`_ for example registration.

Provisioning an Agent
=====================

The process of provisioning agents will create and configure an agent wallet and initialize the agent configuration.
The framework will generate a random Did and Verkey, unless you specify ``AgentSeed`` which is used if you need determinism. 
Length of seed must be 32 characters.

.. code-block:: csharp

    await _provisioningService.ProvisionAgentAsync(
        new ProvisioningConfiguration
        {
            EndpointUri = "http://localhost:5000",
            OwnerName = "My Agent"
        });

Check the `ProvisioningConfiguration.cs
<https://github.com/streetcred-id/agent-framework/blob/master/src/AgentFramework.Core/Models/Wallets/ProvisioningConfiguration.cs>`_
for full configuration details. You can retrieve the generated details like agent Did and Verkey using

.. code-block:: csharp

    var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

***************
Agent Workflows
***************

Before you begin reading any of the topics below, please familiarize youself with the core principles behind Hyperledger Indy.
We suggest that you go over the `Indy SDK Getting Started Guide
<https://github.com/hyperledger/indy-sdk/blob/master/doc/getting-started/getting-started.md>`_.

Models and states
=================

The framework abstracts the main workflows of Indy into a state machine model.
The following models and states are defined:

Connections
-----------

Represented with a ``ConnectionRecord``, this entity describes the pairwise relationship with another party.
The states for this record are:

- ``Invited`` - initially, when creating invitations to connect, the record will be set to this state.
- ``Negotating`` - set after accepting an invitation and sending a request to connect
- ``Connected`` - set when both parties have acknowledged the connection and have a pairwise record of each others DID's

Credentials
-----------

Represented wih a ``CredentialRecord``, this entity holds a reference to issued credential.
While only the party to whom this credential was issued will have the actual credential in their wallet, both the issuer and the holder will
have a CredentialRecord with the associated status for their reference. Credential states:

- ``Offered`` - initial state, when an offer is sent to the holder
- ``Requested`` - the holder has sent a credential request to the issuer
- ``Issued`` - the issuer accepted the credential request and issued a credential
- ``Rejected`` - the issuer rejected the credential request
- ``Revoked`` - the issuer revoked a previously issued credential

Proofs
------

Represented with a ``ProofRecord``, this entity references a proof flow between the holder and verifier. The ``ProofRecord`` contains
information about the proof request as well as the disclosed proof by the holder. Proof states:

- ``Requested`` - initial state when the verifier sends a proof request
- ``Accepted`` - the holder has provided a proof
- ``Rejected`` - the holder rejected providing proof for the request

Schemas and definitions
=======================

Before an issuer can create credentials, they need to register a credential definition for them on the ledger.
Credential definition requires a schema, which can also be registered by the same issuer or it can already be
present on the ledger.

.. code-block:: csharp

    // creates new schema and registers the schema on the ledger
    var schemaId = await _schemaService.CreateSchemaAsync(
        _pool, _wallet, "My-Schema", "1.0", new[] { "FirstName", "LastName", "Email" });

    // to lookup an existing schema on the ledger
    var schemaJson = await _schemaService.LookupSchemaAsync(_pool, schemaId);

Once a ``schemaId`` has been established, an issuer can send their credential definition on the ledger.

.. code-block:: csharp

    var definitionId = await _schemaService.CreateCredentialDefinitionAsync(
        _pool, _wallet, schemaId,
        supportsRevocation: true, 
        maxCredentialCount: 100, 
        tailsBaseUri: new Uri("http://example.com/tails"));

The above code will create ``SchemaRecord`` and ``DefinitionRecord`` in the issuer wallet that can be looked up using the
``ISchemaService``.

.. note:: The wallet instance passed to the above methods must be provisioned as Issuer i.e. using ``CreateIssuer=true`` or 
    otherwise a Did must exist in the wallet. Additionaly, the DID must be registered on the ledger as ``TRUST_ANCHOR``.


To retrieve all schemas or definitions registered with this agent, use:

.. code-block:: csharp

    var schemas = await _schemaService.ListSchemasAsync(_wallet);
    var definitions = await _schemaService.ListCredentialDefinitionsAsync(_wallet);

    // To get a single record
    var definition = await _schemaService.GetCredentialDefinitionAsync(wallet, definitionId);

Establishing secure connection
==============================

Sending invitations
-------------------

Negotating connection
---------------------

Credential issuance
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