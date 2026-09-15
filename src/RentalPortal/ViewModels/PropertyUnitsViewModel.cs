namespace RentalPortal.ViewModels;

public class PropertyUnitsViewModel
{
    public int PropertyId { get; set; }
    public IReadOnlyList<PropertyUnitRowViewModel> Units { get; set; } = [];
}

public class PropertyUnitRowViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public bool UnitTypeIsActive { get; set; }
    public bool IsAvailable { get; set; }
}

public class StatusHistoryItemViewModel
{
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class ResidenceListViewModel
{
    public int ApplicationId { get; set; }
    public bool CanEdit { get; set; }
    public IReadOnlyList<ResidenceRowViewModel> Residences { get; set; } = [];
}
