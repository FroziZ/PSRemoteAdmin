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

    public CredentialResult GetRunAsCredential(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.RunAsUsername))
            return new CredentialResult(null, false);

        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(VaultResource, settings.RunAsUsername);
            cred.RetrievePassword();
            var secure = new SecureString();
            foreach (char c in cred.Password)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return new CredentialResult(new PSCredential(settings.RunAsUsername, secure), false);
        }
        catch
        {
            return new CredentialResult(null, true);
        }
    }

    public void StorePassword(string username, SecureString password)
    {
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
        catch { /* already gone */ }
    }

    private static string SecureStringToString(SecureString secure)
    {
        var ptr = Marshal.SecureStringToBSTR(secure);
        try { return Marshal.PtrToStringBSTR(ptr); }
        finally { Marshal.ZeroFreeBSTR(ptr); }
    }
}
