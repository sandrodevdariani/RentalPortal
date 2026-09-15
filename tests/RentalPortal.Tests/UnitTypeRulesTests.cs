using FluentAssertions;
using RentalPortal.Domain;

namespace RentalPortal.Tests;

public class UnitTypeRulesTests
{
    [Fact]
    public void Active_type_can_be_assigned_to_any_unit()
    {
        UnitTypeRules.CanAssign(isActive: true, unitTypeId: 2, currentUnitTypeId: null).Should().BeTrue();
        UnitTypeRules.CanAssign(isActive: true, unitTypeId: 2, currentUnitTypeId: 1).Should().BeTrue();
    }

    [Fact]
    public void Inactive_type_can_remain_on_the_unit_that_already_uses_it()
    {
        UnitTypeRules.CanAssign(isActive: false, unitTypeId: 5, currentUnitTypeId: 5).Should().BeTrue();
    }

    [Fact]
    public void Inactive_type_cannot_be_selected_for_a_different_or_new_unit()
    {
        UnitTypeRules.CanAssign(isActive: false, unitTypeId: 5, currentUnitTypeId: null).Should().BeFalse();
        UnitTypeRules.CanAssign(isActive: false, unitTypeId: 5, currentUnitTypeId: 2).Should().BeFalse();
    }
}
