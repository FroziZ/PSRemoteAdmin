using Microsoft.Extensions.Logging;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Security.Credentials;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Services;

public record CredentialResult(PSCredential? Credential, bool PasswordMissing);

public class CredentialService
{
    private const string VaultResource = "PSRemoteAdmin";
    private readonly ILogger<CredentialService> _logger;

    public CredentialService(ILogger<CredentialService> logger)
    {
        _logger = logger;
    }

    public CredentialResult GetRunAsCredential(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.RunAsUsername))
            return new CredentialResult(null, false);

        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(VaultResource, settings.RunAsUsername);
            cred.RetrievePassword();
            // Note: cred.Password is a plain string (WinRT API limitation).
            // We copy it into SecureString immediately and let cred go out of scope.
            var secure = new SecureString();
            foreach (char c in cred.Password)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return new CredentialResult(new PSCredential(settings.RunAsUsername, secure), false);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException ||
                                    ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            // Credential not in vault — expected when first configured
            return new CredentialResult(null, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error reading credential for {Username} from vault",
                settings.RunAsUsername);
            return new CredentialResult(null, true);
        }
    }

    public void StorePassword(string username, SecureString password)
    {
        // Remove existing entry first — PasswordVault.Add throws on duplicate
        ClearPassword(username);
        var vault = new PasswordVault();
        var plain = SecureStringToString(password);
        vault.Add(new PasswordCredential(VaultResource, username, plain));
    }

    public void ClearPassword(string username)
    {
        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(VaultResource, username);
            vault.Remove(cred);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException)
        {
            // Credential not found — nothing to clear
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error clearing credential for {Username} from vault", username);
        }
    }

    private static string SecureStringToString(SecureString secure)
    {
        var ptr = Marshal.SecureStringToBSTR(secure);
        try { return Marshal.PtrToStringBSTR(ptr); }
        finally { Marshal.ZeroFreeBSTR(ptr); }
    }
}
