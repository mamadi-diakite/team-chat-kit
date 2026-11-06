namespace TeamChat.AspNetCore.Tests;

public sealed class TeamChatRegistrationTests
{
    [Fact]
    public void AddTeamChat_rejects_missing_arguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => TeamChatServiceCollectionExtensions.AddTeamChat<TestIdentityAdapter>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTeamChat<TestIdentityAdapter>(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTeamChat<TestIdentityAdapter>(_ => { }));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTeamChat<TestIdentityAdapter>(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.RoutePrefix = " ";
        }));
    }

    [Fact]
    public void MapTeamChat_rejects_null_endpoints()
    {
        Assert.Throws<ArgumentNullException>(() => TeamChatEndpointRouteBuilderExtensions.MapTeamChat(null!));
    }

    [Fact]
    public void AddTeamChat_registers_the_options_the_adapter_and_the_storage()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddTeamChat<TestIdentityAdapter>(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.TablePrefix = "tc_";
        }));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Equal(TeamChatOptions.DefaultRoutePrefix, provider.GetRequiredService<TeamChatOptions>().RoutePrefix);
        Assert.Equal("tc_", provider.GetRequiredService<ChatStorageOptions>().TablePrefix);
        Assert.IsType<TestIdentityAdapter>(scope.ServiceProvider.GetRequiredService<IChatIdentityAdapter>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CurrentChatUserResolver>());
    }
}
