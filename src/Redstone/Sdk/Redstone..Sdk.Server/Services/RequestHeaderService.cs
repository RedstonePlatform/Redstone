using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Redstone.Sdk.Server.Exceptions;

namespace Redstone.Sdk.Server.Services
{
    public class RedstoneHeader
    {
        public string Scheme { get; set; }
        public string Value { get; set; }
    }

    public static class RedstoneContants
    {
        public const string RedstoneHexScheme = "hex";
        public const string RedstoneTokenScheme = "token";
    }

    public interface IRequestHeaderService
    {
        string GetRedstoneHeader(string scheme);
    }

    public class RequestHeaderService : IRequestHeaderService
    {
        private const string RedstoneHeaderName = "Redstone";
        private const string RedstoneHexSchemeName = "hex";
        private const string RedstoneTokenSchemeName = "token";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestHeaderService(IHttpContextAccessor httpContextAccessor, INetworkService networkService)
        {
            this._httpContextAccessor = httpContextAccessor;
            networkService.InitializeNetwork(true);
        }

        public string GetRedstoneHeader(string scheme)
        {
            if (!scheme.Equals(RedstoneTokenSchemeName) && !scheme.Equals(RedstoneHexSchemeName))
                throw new RequestHeaderServiceException("Unsupported redstone header scheme requested");

            var headers = this._httpContextAccessor.HttpContext.Request.Headers;

            if (!headers.ContainsKey(RedstoneHeaderName))
            {
                throw new RequestHeaderServiceException("Redstone header not present");
            }

            if (!AuthenticationHeaderValue.TryParse(headers[RedstoneHeaderName], out AuthenticationHeaderValue headerValue))
            {
                throw new RequestHeaderServiceException("Redstone header invalid");
            }

            if (scheme.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return headerValue.Parameter;
            }
            else
            {
                throw new RequestHeaderServiceException("Unsupported Redstone header scheme found");
            }
        }
    }
}