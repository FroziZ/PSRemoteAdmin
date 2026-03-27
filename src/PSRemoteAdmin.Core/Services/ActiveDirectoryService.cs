using System.DirectoryServices;
using Microsoft.Extensions.Logging;
using PSRemoteAdmin.Core.Exceptions;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Core.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly AppSettings _settings;
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(AppSettings settings, ILogger<ActiveDirectoryService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task<IReadOnlyList<AdNode>> GetRootNodesAsync() =>
        GetChildrenInternalAsync(_settings.LdapConnectionString);

    public Task<IReadOnlyList<AdNode>> GetChildrenAsync(string distinguishedName) =>
        GetChildrenInternalAsync($"LDAP://{distinguishedName}");

    public async Task<IReadOnlyList<AdNode>> GetComputersRecursiveAsync(string distinguishedName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var result = new List<AdNode>();
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = "(objectClass=computer)",
                    SearchScope = SearchScope.Subtree,
                    PageSize = 1000
                };
                searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName", "dNSHostName" });
                foreach (SearchResult sr in searcher.FindAll())
                {
                    result.Add(MapComputer(sr));
                }
                return (IReadOnlyList<AdNode>)result;
            }
            catch (Exception ex) when (ex is not ActiveDirectoryServiceException)
            {
                throw new ActiveDirectoryServiceException($"Failed to enumerate computers under '{distinguishedName}'.", ex);
            }
        });
    }

    public async Task TestConnectionAsync(string ldapConnectionString, string domain)
    {
        await Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry(ldapConnectionString);
                _ = entry.Properties.Count; // force a bind
            }
            catch (Exception ex)
            {
                throw new ActiveDirectoryServiceException(
                    $"Cannot connect to '{ldapConnectionString}': {ex.Message}", ex);
            }
        });
    }

    private async Task<IReadOnlyList<AdNode>> GetChildrenInternalAsync(string ldapPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                var result = new List<AdNode>();
                using var entry = new DirectoryEntry(ldapPath);
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = "(|(objectClass=organizationalUnit)(objectClass=computer))",
                    SearchScope = SearchScope.OneLevel,
                    PageSize = 1000
                };
                searcher.PropertiesToLoad.AddRange(
                    new[] { "name", "distinguishedName", "objectClass", "dNSHostName" });

                foreach (SearchResult sr in searcher.FindAll())
                {
                    var classes = sr.Properties["objectClass"];
                    bool isComputer = false;
                    foreach (object c in classes)
                        if (c.ToString() == "computer") { isComputer = true; break; }

                    result.Add(isComputer ? MapComputer(sr) : MapOu(sr));
                }
                return (IReadOnlyList<AdNode>)result.OrderBy(n => n.NodeType).ThenBy(n => n.Name).ToList();
            }
            catch (Exception ex) when (ex is not ActiveDirectoryServiceException)
            {
                throw new ActiveDirectoryServiceException($"Failed to query Active Directory at '{ldapPath}'.", ex);
            }
        });
    }

    private static AdNode MapComputer(SearchResult sr)
    {
        var dn = sr.Properties["distinguishedName"][0]?.ToString() ?? string.Empty;
        return new AdNode
        {
            Name = sr.Properties["name"][0]?.ToString() ?? dn,
            DistinguishedName = dn,
            NodeType = AdNodeType.Computer,
            DnsHostName = sr.Properties["dNSHostName"].Count > 0
                ? sr.Properties["dNSHostName"][0]?.ToString()
                : null,
            HasChildren = false
        };
    }

    private static AdNode MapOu(SearchResult sr)
    {
        var dn = sr.Properties["distinguishedName"][0]?.ToString() ?? string.Empty;
        return new AdNode
        {
            Name = sr.Properties["name"][0]?.ToString() ?? dn,
            DistinguishedName = dn,
            NodeType = AdNodeType.OrganizationalUnit,
            HasChildren = true
        };
    }
}
