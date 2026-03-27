using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Core.Services;

public interface IActiveDirectoryService
{
    /// <summary>Returns root OUs/computers directly beneath the configured domain.</summary>
    /// <exception cref="PSRemoteAdmin.Core.Exceptions.ActiveDirectoryServiceException"/>
    Task<IReadOnlyList<AdNode>> GetRootNodesAsync();

    /// <summary>Returns immediate OU and computer children of the given DN.</summary>
    /// <exception cref="PSRemoteAdmin.Core.Exceptions.ActiveDirectoryServiceException"/>
    Task<IReadOnlyList<AdNode>> GetChildrenAsync(string distinguishedName);

    /// <summary>Returns all computer objects recursively beneath the given OU DN.</summary>
    /// <exception cref="PSRemoteAdmin.Core.Exceptions.ActiveDirectoryServiceException"/>
    Task<IReadOnlyList<AdNode>> GetComputersRecursiveAsync(string distinguishedName);

    /// <summary>
    /// Probes the given LDAP connection with the supplied parameters (not current IOptions).
    /// Throws ActiveDirectoryServiceException on failure. Used by SettingsViewModel.TestConnectionCommand.
    /// </summary>
    Task TestConnectionAsync(string ldapConnectionString, string domain);
}
