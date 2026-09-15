using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Domain;
using RentalPortal.Entities;

namespace RentalPortal.Data;

public static class DatabaseSeeder
{
    public const string DemoPassword = "Password1!";
    public const string ManagerEmail = "manager@alder.test";
    public const string ApplicantEmail = "applicant@alder.test";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var clock = services.GetRequiredService<IClock>();

        await EnsureRolesAsync(roles);
        await EnsureDemoUsersAsync(users);
        await EnsureUnitTypesAsync(db);
        await EnsurePropertiesAndUnitsAsync(db);
        await EnsureApplicationsAsync(db, users, clock);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var name in new[] { Roles.Applicant, Roles.PropertyManager })
        {
            if (!await roles.RoleExistsAsync(name))
            {
                await roles.CreateAsync(new IdentityRole(name));
            }
        }
    }

    private static async Task EnsureDemoUsersAsync(UserManager<ApplicationUser> users)
    {
        await EnsureUserAsync(users, ManagerEmail, "Helena", "Alder", Roles.PropertyManager, "2065550100");
        await EnsureUserAsync(users, ApplicantEmail, "Jonah", "Reed", Roles.Applicant, "2065550142");

        Randomizer.Seed = new Random(41216);
        var faker = new Faker("en_US");

        for (var i = 1; i <= 3; i++)
        {
            var email = $"pm{i}@alder.test";
            if (await users.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            await EnsureUserAsync(
                users,
                email,
                faker.Name.FirstName(),
                faker.Name.LastName(),
                Roles.PropertyManager,
                faker.Phone.PhoneNumber("###-###-####"));
        }

        for (var i = 1; i <= 8; i++)
        {
            var email = $"applicant{i}@alder.test";
            if (await users.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            await EnsureUserAsync(
                users,
                email,
                faker.Name.FirstName(),
                faker.Name.LastName(),
                Roles.Applicant,
                faker.Phone.PhoneNumber("###-###-####"));
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> users,
        string email,
        string firstName,
        string lastName,
        string role,
        string phone)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, role))
            {
                await users.AddToRoleAsync(existing, role);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone
        };

        var result = await users.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await users.AddToRoleAsync(user, role);
    }

    private static async Task EnsureUnitTypesAsync(AppDbContext db)
    {
        async Task AddIfMissing(string name, bool active)
        {
            if (await db.UnitTypes.AnyAsync(t => t.Name == name))
            {
                return;
            }

            db.UnitTypes.Add(new UnitType { Name = name, IsActive = active });
        }

        await AddIfMissing("Studio", true);
        await AddIfMissing("One Bedroom", true);
        await AddIfMissing("Two Bedroom", true);
        await AddIfMissing("Three Bedroom", true);
        await AddIfMissing("Penthouse", false);
        await AddIfMissing("Garden Suite", false);
        await db.SaveChangesAsync();
    }

    private static async Task EnsurePropertiesAndUnitsAsync(AppDbContext db)
    {
        if (await db.Properties.AnyAsync())
        {
            return;
        }

        var types = await db.UnitTypes.ToDictionaryAsync(t => t.Name, t => t.Id);

        var properties = new[]
        {
            new Property
            {
                Name = "Harbor Court",
                Address = "88 Alaskan Way",
                City = "Seattle",
                State = "WA",
                ZipCode = "98104",
                Units =
                [
                    Unit("101", 0, 1650, types["Studio"]),
                    Unit("204", 1, 2100, types["One Bedroom"]),
                    Unit("305", 2, 2750, types["Two Bedroom"]),
                    Unit("PH1", 3, 5200, types["Penthouse"])
                ]
            },
            new Property
            {
                Name = "Oak & Pine Residences",
                Address = "412 Pine Street",
                City = "Seattle",
                State = "WA",
                ZipCode = "98101",
                Units =
                [
                    Unit("12A", 1, 1950, types["One Bedroom"]),
                    Unit("12B", 1, 1985, types["One Bedroom"]),
                    Unit("21C", 2, 2600, types["Two Bedroom"])
                ]
            },
            new Property
            {
                Name = "The Alder House",
                Address = "1501 15th Avenue",
                City = "Seattle",
                State = "WA",
                ZipCode = "98122",
                Units =
                [
                    Unit("B1", 0, 1425, types["Studio"]),
                    Unit("3N", 2, 2490, types["Two Bedroom"]),
                    Unit("4N", 3, 3100, types["Three Bedroom"])
                ]
            },
            new Property
            {
                Name = "Riverside Lofts",
                Address = "2200 Westlake Avenue",
                City = "Seattle",
                State = "WA",
                ZipCode = "98121",
                Units =
                [
                    Unit("L1", 1, 2300, types["One Bedroom"]),
                    Unit("L2", 2, 2900, types["Two Bedroom"])
                ]
            }
        };

        db.Properties.AddRange(properties);
        await db.SaveChangesAsync();
    }

    private static Unit Unit(string number, int bedrooms, decimal rent, int typeId) => new()
    {
        UnitNumber = number,
        Bedrooms = bedrooms,
        MonthlyRent = rent,
        UnitTypeId = typeId
    };

    private static async Task EnsureApplicationsAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        IClock clock)
    {
        if (await db.Applications.AnyAsync())
        {
            return;
        }

        Randomizer.Seed = new Random(41216);
        var faker = new Faker("en_US");

        var applicant = await users.FindByEmailAsync(ApplicantEmail)
            ?? throw new InvalidOperationException("Demo applicant missing.");
        var manager = await users.FindByEmailAsync(ManagerEmail)
            ?? throw new InvalidOperationException("Demo manager missing.");

        var extraApplicants = new List<ApplicationUser>();
        for (var i = 1; i <= 8; i++)
        {
            extraApplicants.Add(await users.FindByEmailAsync($"applicant{i}@alder.test")
                ?? throw new InvalidOperationException("Seeded applicant missing."));
        }

        var units = await db.Units.Include(u => u.Property).OrderBy(u => u.Id).ToListAsync();
        if (units.Count < 6)
        {
            throw new InvalidOperationException("Seeded units missing.");
        }

        RentalApplication Make(
            ApplicationUser user,
            Unit unit,
            ApplicationStatus status,
            bool infoSaved = true,
            bool historySaved = true)
        {
            var app = new RentalApplication
            {
                Unit = unit,
                ApplicantUserId = user.Id,
                Status = status,
                FullName = user.FullName,
                Phone = user.PhoneNumber ?? faker.Phone.PhoneNumber("###-###-####"),
                Email = user.Email ?? faker.Internet.Email(),
                CurrentAddress = faker.Address.FullAddress(),
                ApplicantInfoSaved = infoSaved,
                ResidenceHistorySaved = historySaved,
                CreatedAt = clock.UtcNow.AddDays(-14),
                UpdatedAt = clock.UtcNow.AddDays(-1)
            };

            if (historySaved)
            {
                app.Residences.Add(new Residence
                {
                    Address = faker.Address.FullAddress(),
                    LandlordName = faker.Name.FullName(),
                    LandlordPhone = faker.Phone.PhoneNumber("###-###-####"),
                    MoveInDate = DateOnly.FromDateTime(faker.Date.Past(4)),
                    MoveOutDate = DateOnly.FromDateTime(faker.Date.Past(1))
                });
            }

            app.StatusChanges.Add(new ApplicationStatusChange
            {
                FromStatus = null,
                ToStatus = ApplicationStatus.Draft,
                ChangedByUserId = user.Id,
                ChangedAt = app.CreatedAt,
                Comment = "Application created."
            });

            if (status != ApplicationStatus.Draft)
            {
                app.StatusChanges.Add(new ApplicationStatusChange
                {
                    FromStatus = ApplicationStatus.Draft,
                    ToStatus = ApplicationStatus.Submitted,
                    ChangedByUserId = user.Id,
                    ChangedAt = app.CreatedAt.AddDays(1)
                });
            }

            if (status is ApplicationStatus.Returned or ApplicationStatus.Approved or ApplicationStatus.Denied)
            {
                var outcome = status switch
                {
                    ApplicationStatus.Returned => ReviewOutcome.Return,
                    ApplicationStatus.Approved => ReviewOutcome.Approve,
                    _ => ReviewOutcome.Deny
                };
                app.StatusChanges.Add(new ApplicationStatusChange
                {
                    FromStatus = ApplicationStatus.Submitted,
                    ToStatus = status,
                    Outcome = outcome,
                    ChangedByUserId = manager.Id,
                    ChangedAt = app.CreatedAt.AddDays(3),
                    Comment = status == ApplicationStatus.Returned
                        ? "Please add a more recent landlord reference."
                        : status == ApplicationStatus.Denied
                            ? "Income documentation did not meet the property requirement."
                            : "Approved. Welcome to the building."
                });
            }

            if (status == ApplicationStatus.Withdrawn)
            {
                app.StatusChanges.Add(new ApplicationStatusChange
                {
                    FromStatus = ApplicationStatus.Submitted,
                    ToStatus = ApplicationStatus.Withdrawn,
                    ChangedByUserId = user.Id,
                    ChangedAt = app.CreatedAt.AddDays(2),
                    Comment = "Applicant withdrew."
                });
            }

            if (status == ApplicationStatus.Approved)
            {
                var start = clock.Today.AddMonths(-2);
                app.Lease = new Lease
                {
                    Unit = unit,
                    TenantUserId = user.Id,
                    StartDate = start,
                    EndDate = ApplicationRules.LeaseEndDate(start)
                };
            }

            return app;
        }

        db.Applications.AddRange(
            Make(applicant, units[0], ApplicationStatus.Draft, infoSaved: false, historySaved: false),
            Make(applicant, units[1], ApplicationStatus.Returned),
            Make(extraApplicants[0], units[2], ApplicationStatus.Submitted),
            Make(extraApplicants[1], units[3], ApplicationStatus.Approved),
            Make(extraApplicants[2], units[4], ApplicationStatus.Denied),
            Make(extraApplicants[3], units[5], ApplicationStatus.Withdrawn),
            Make(extraApplicants[4], units[6], ApplicationStatus.Submitted),
            Make(extraApplicants[5], units[7], ApplicationStatus.Draft));

        await db.SaveChangesAsync();
    }
}
