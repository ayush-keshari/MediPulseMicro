namespace Shared.Constants;

// const string fields are compile-time constants.
// This means they can be used directly in [Authorize(Roles = Roles.Admin)] attributes.
public static class Roles
{
    public const string Admin = "Admin";
    public const string SupplyManager = "SupplyManager";
    public const string PharmacyManager = "PharmacyManager";
    public const string DeviceManager = "DeviceManager";
    public const string ProcurementOfficer = "ProcurementOfficer";
    public const string ColdChainOperator = "ColdChainOperator";
    public const string Nurse = "Nurse";
    public const string ComplianceOfficer = "ComplianceOfficer";
    public const string Unassigned = "Unassigned";
}
