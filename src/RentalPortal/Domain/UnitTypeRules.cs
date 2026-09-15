namespace RentalPortal.Domain;

public static class UnitTypeRules
{
    /// <summary>
    /// Inactive types may remain on a unit that already uses them, but cannot be selected otherwise.
    /// </summary>
    public static bool CanAssign(bool isActive, int unitTypeId, int? currentUnitTypeId) =>
        isActive || currentUnitTypeId == unitTypeId;
}
