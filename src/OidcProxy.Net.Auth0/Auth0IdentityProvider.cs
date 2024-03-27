﻿using System.Web;
using OidcProxy.Net.IdentityProviders;
using OidcProxy.Net.Logging;
using OidcProxy.Net.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;

namespace OidcProxy.Net.Auth0;

public class Auth0IdentityProvider(
    ILogger logger,
    IMemoryCache cache,
    HttpClient client,
    Auth0Config config)
    : OpenIdConnectIdentityProvider(logger,
        cache,
        client,
        MapConfiguration(config))
{
    public override async Task<AuthorizeRequest> GetAuthorizeUrlAsync(string redirectUri)
    {
        var result = await base.GetAuthorizeUrlAsync(redirectUri);

        var audience = HttpUtility.UrlEncode(config.Audience);
        var authorizeRequestWithAudience = $"{result.AuthorizeUri}&audience={audience}";

        return new AuthorizeRequest(new Uri(authorizeRequestWithAudience), result.CodeVerifier);
    }

    public override Task RevokeAsync(string token, string traceIdentifier)
    {
        // I.l.e.: Auth0 does not support token revocation
        
        return Task.CompletedTask;
    }

    protected override Task<Uri> BuildEndSessionUri(string? idToken, string redirectUri)
    {
        // Auth0 does not define their end_session_endpoint in the well-known/openid-configuration

        var federated = config.FederatedLogout ? "?federated" : string.Empty;
        var endSessionUrl = $"https://{config.Domain}/oidc/logout{federated}";
        var endSessionUri = new Uri(endSessionUrl);
        
        return Task.FromResult(endSessionUri);
    }

    private static OpenIdConnectConfig MapConfiguration(Auth0Config config)
    {
        return new OpenIdConnectConfig
        {
            Authority = $"https://{config.Domain}",
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            Scopes = config.Scopes
        };
    }
}