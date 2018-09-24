using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Streetcred.Sdk.Extensions
{
    public class TailsMiddleware
    {
        private readonly RequestDelegate _next;

        public TailsMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
        }
    }
}