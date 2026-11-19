// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Chronicle.Api.Services;

/// <summary>
/// Swagger UI's Authorize button, with a Keycloak login: Swagger calls the API straight, not through the UI's proxy, so it
/// carries no session cookie and sends a bearer token instead (code flow with PKCE, for <see cref="LoginOptions.SwaggerClientId"/>).
/// </summary>
public static class SwaggerLogin
{
    private const string Scheme = "keycloak";

    /// <summary>The OpenAPI document says every operation takes Keycloak's OAuth2 code flow.</summary>
    public static OpenApiOptions AddKeycloakSecurity(this OpenApiOptions options, LoginOptions login)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[Scheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(login.AuthorizationUrlOrDefault),
                        TokenUrl = new Uri(login.TokenUrlOrDefault),
                        Scopes = new Dictionary<string, string> { ["openid"] = "Sign in", ["profile"] = "Your name" }
                    }
                }
            };
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(Scheme, document)] = ["openid", "profile"] });
            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>Swagger UI signs in as the API's own client, with PKCE and no secret.</summary>
    public static void UseKeycloakLogin(this SwaggerUIOptions options, LoginOptions login)
    {
        options.OAuthClientId(login.SwaggerClientId);
        options.OAuthUsePkce();
        options.OAuthScopes("openid", "profile");
    }
}
