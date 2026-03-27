namespace PSRemoteAdmin.Core.Models;

public class AdNode
{
    public required string Name { get; init; }
    public required string DistinguishedName { get; init; }
    public AdNodeType NodeType { get; init; }
    public string? DnsHostName { get; init; }
    public bool HasChildren { get; init; }
}

public enum AdNodeType
{
    OrganizationalUnit,
    Computer
}
