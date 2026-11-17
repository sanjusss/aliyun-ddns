namespace AliyunDdns.Domain;

public enum DnsOperationKind
{
    None,
    Skip,
    Add,
    Update,
    Delete
}

public sealed record DnsOperation
{
    public required DnsOperationKind Kind { get; init; }

    public required DnsRecordType Type { get; init; }

    public required string Reason { get; init; }

    public DnsRecord? ExistingRecord { get; init; }

    public string? DesiredAddress { get; init; }
}

public sealed record UpdatePlan
{
    public required ResolvedTarget Target { get; init; }

    public IReadOnlyList<DnsOperation> Operations { get; init; } = [];

    public string Domain => Target.Domain;

    public bool HasChanges => Operations.Any(static operation =>
        operation.Kind is DnsOperationKind.Add or DnsOperationKind.Update or DnsOperationKind.Delete);
}
