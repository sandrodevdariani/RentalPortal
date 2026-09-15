namespace RentalPortal.ViewModels;

public class AvailableUnitViewModel
{
    public int UnitId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string UnitType { get; set; } = string.Empty;
}
