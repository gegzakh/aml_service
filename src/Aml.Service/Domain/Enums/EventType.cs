namespace AmlOps.Backend.Domain.Enums;

public static class EventType
{
    public const string CaseCreated = "CaseCreated";
    public const string StatusChanged = "StatusChanged";
    public const string Assigned = "Assigned";
    public const string DecisionSet = "DecisionSet";
    public const string CommentAdded = "CommentAdded";
    public const string EvidenceAdded = "EvidenceAdded";
    public const string CaseApproved = "CaseApproved";
    public const string CaseClosed = "CaseClosed";
    public const string EvidencePackExported = "EvidencePackExported";
    public const string AlertsImported = "AlertsImported";
}
