﻿using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using GenerativeAI.Core;
using Google.Apis.Auth.OAuth2;

namespace GenerativeAI.Authenticators;

/// <summary>
/// Authenticator that uses Google Service Account credentials for authentication with Google services.
/// </summary>
public class GoogleServiceAccountAuthenticator : BaseAuthenticator
{
    private readonly List<string> _scopes =
    [
        "https://www.googleapis.com/auth/cloud-platform",
        "https://www.googleapis.com/auth/generative-language.retriever",
        "https://www.googleapis.com/auth/generative-language.tuning",
    ];

    private string _clientFile = "client_secret.json";
    private string _certificateFile = "key.p12";
    private string _certificatePassphrase = null!;

    private ServiceAccountCredential _credential;

    /// <summary>
    /// Initializes a new instance of the GoogleServiceAccountAuthenticator class.
    /// </summary>
    /// <param name="serviceAccountEmail">The service account email address.</param>
    /// <param name="certificate">Optional path to the certificate file. If null, uses default.</param>
    /// <param name="passphrase">Optional passphrase for the certificate.</param>
    public GoogleServiceAccountAuthenticator(string serviceAccountEmail, string? certificate = null,
        string? passphrase = null)
    {
        X509Certificate2? x509Certificate = null;
        try
        {
#pragma warning disable CA2000 // Certificate ownership is transferred to ServiceAccountCredential
#if NET9_0_OR_GREATER
#pragma warning disable SYSLIB0057 // X509Certificate2 constructor is obsolete
#endif
            x509Certificate = new X509Certificate2(
                certificate ?? _certificateFile,
                passphrase ?? _certificatePassphrase,
                X509KeyStorageFlags.Exportable);
#if NET9_0_OR_GREATER
#pragma warning restore SYSLIB0057
#endif
#pragma warning restore CA2000
            _credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(serviceAccountEmail)
                {
                    Scopes = _scopes
                }.FromCertificate(x509Certificate));
        }
        catch
        {
            x509Certificate?.Dispose();
            throw;
        }
    }
    
    /// <summary>
    /// Authenticator class for Google services using a service account.
    /// </summary>
    public GoogleServiceAccountAuthenticator(string? credentialFile)
    {
        using var stream = File.OpenRead(credentialFile ?? _clientFile);
        _credential = ServiceAccountCredential.FromServiceAccountData(stream);
        _credential.Scopes = _scopes;
    }

    /// <summary>
    /// Initializes a new instance of the GoogleServiceAccountAuthenticator class using a stream containing service account data.
    /// </summary>
    /// <param name="stream">Stream containing the service account JSON data.</param>
    public GoogleServiceAccountAuthenticator(Stream stream)
    {
        _credential = ServiceAccountCredential.FromServiceAccountData(stream);
        _credential.Scopes = _scopes;
    }

    /// <inheritdoc/>
    public override async Task<AuthTokens?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await _credential.GetAccessTokenForRequestAsync(cancellationToken:cancellationToken).ConfigureAwait(false);

        if(string.IsNullOrEmpty(token))
            throw new AuthenticationException("Failed to get access token.");
        var tokenInfo = await GetTokenInfo(token).ConfigureAwait(false);
        if(tokenInfo == null)
            throw new AuthenticationException("Failed to get access token.");
        return tokenInfo;
    }

    /// <inheritdoc/>
    public override async Task<AuthTokens?> RefreshAccessTokenAsync(AuthTokens token, CancellationToken cancellationToken = default)
    {
        return await GetAccessTokenAsync(cancellationToken:cancellationToken).ConfigureAwait(false);
    }
}