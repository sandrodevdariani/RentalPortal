namespace RentalPortal.ViewModels;

public class LandingViewModel
{
    public int PropertyCount { get; set; }
    public int AvailableCount { get; set; }
    public IReadOnlyList<AvailableUnitViewModel> Featured { get; set; } = [];
}
