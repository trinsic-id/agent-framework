using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Decorators.Threading
{
    /// <summary>
    /// Message threading extensions.
    /// </summary>
    public static class ThreadDecoratorExtensions
    {
        /// <summary>
        /// Threading decorator extension.
        /// </summary>
        public static string DecoratorIdentifier => "thread";

        /// <summary>
        /// Created a new threaded message response
        /// </summary>
        /// <param name="message">The message to thread from.</param>
        public static T CreateThreadedReply<T>(this AgentMessage message) where T : AgentMessage, new ()
        {
            var newMsg = new T();
            newMsg.ThreadMessage(message);
            return newMsg;
        }

        /// <summary>
        /// Threads the current message from a previous message.
        /// </summary>
        /// <param name="message">The message to add threading to.</param>
        /// <param name="previousMessage">The message to thread from.</param>
        public static void ThreadFrom(this AgentMessage message, AgentMessage previousMessage)
        {
            bool hasThreadBlock = false;
            try
            {
                message.GetDecorator<ThreadDecorator>(DecoratorIdentifier);
                hasThreadBlock = true;
            }
            catch (AgentFrameworkException) { }

            if (hasThreadBlock)
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot thread message when it already has a valid thread decorator");

            message.ThreadMessage(previousMessage);
        }

        private static void ThreadMessage(this AgentMessage messageToThread, AgentMessage messageToThreadFrom)
        {
            ThreadDecorator previousMessageThreadContext = null;
            try
            {
                previousMessageThreadContext = messageToThreadFrom.GetDecorator<ThreadDecorator>(DecoratorIdentifier);
            }
            catch (AgentFrameworkException) { }

            ThreadDecorator currentThreadContext;
            if (previousMessageThreadContext != null)
            {
                currentThreadContext = new ThreadDecorator
                {
                    ParentThreadId = previousMessageThreadContext.ParentThreadId,
                    ThreadId = previousMessageThreadContext.ThreadId
                };
            }
            else
            {
                currentThreadContext = new ThreadDecorator
                {
                    ThreadId = messageToThreadFrom.Id
                };
            }


            messageToThread.AddDecorator(currentThreadContext, DecoratorIdentifier);
        }
    }
}
