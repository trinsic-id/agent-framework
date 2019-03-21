﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFramework.TestHarness.Mock
{
    public class MockAgentHttpHandler : HttpMessageHandler
    {
        public MockAgentHttpHandler(Action<byte[]> callback)
        {
            Callback = callback;
        }

        public Action<byte[]> Callback { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
            {
                throw new Exception("Invalid http method");
            }
            Callback(await request.Content.ReadAsByteArrayAsync());
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
