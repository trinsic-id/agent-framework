*********************
Getting Started Guide
*********************

Overview
=========

This getting started guide will show you how to help alice get and present a credential. 


Setting up environment
======================
Install visual Studio
Install dotnet sdk 2.2

Create Project
==============

Open Visual Studio and select new project, then choose Web Application (Model-View-Controller):

.. image:: _static/images/choose_template.png
   :width: 500

Select the .NET Core 2.2, then name your project. I've named mine MyAgent:

.. image:: _static/images/configure_agent.png
   :width: 500


Install Required Packages
=========================

Follow the instructions in installation.rst to install the AgentFramework.Core package into your project. 

Continue with the instructions to build libindy and move it into your PATH


Configure Agent
===============

Create a file name ``SimpleWebAgent.cs`` in the main directory

This file will inherit from the AgentBase class in the AgentFramework, and it extends the IAgent Interface. 
This interface includes only one function named ``Task<MessageResponse>ProcessAsync(IAgentContext context, MessageContext messageContext)``
This will process any message that is sent to the agent's endpoint. 






