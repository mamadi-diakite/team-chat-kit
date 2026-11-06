namespace TeamChat.DemoHost.Api.Seeding;

public sealed class DemoDataSeeder(DemoHostDbContext db, IdentityPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "TestDemo123!";

    /// <summary>Fictional organizations and people. All names are made up.</summary>
    public static IReadOnlyList<(string OrganizationName, string Email, string DisplayName)> Accounts { get; } =
    [
        ("Bluebird Design", "emma.laurent@example.com", "Emma Laurent"),
        ("Bluebird Design", "hugo.bernard@example.com", "Hugo Bernard"),
        ("Bluebird Design", "chloe.petit@example.com", "Chloe Petit"),
        ("Harbor Logistics", "liam.moreau@example.com", "Liam Moreau"),
        ("Harbor Logistics", "sara.dubois@example.com", "Sara Dubois"),
        ("Pine Valley Clinic", "noah.girard@example.com", "Noah Girard"),
    ];

    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var passwordHash = passwordHasher.Hash(DemoPassword);

        foreach (var members in Accounts.GroupBy(a => a.OrganizationName))
        {
            var organization = new Organization(members.Key, now);
            db.Organizations.Add(organization);
            foreach (var member in members)
            {
                db.Users.Add(new UserAccount(member.Email, member.DisplayName, passwordHash, organization.Id, now));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
