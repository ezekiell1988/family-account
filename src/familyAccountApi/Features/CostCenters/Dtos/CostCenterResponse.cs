namespace FamilyAccountApi.Features.CostCenters.Dtos;

public sealed record CostCenterResponse(
    int    IdCostCenter,
    string CodeCostCenter,
    string NameCostCenter,
    bool   IsActive);
