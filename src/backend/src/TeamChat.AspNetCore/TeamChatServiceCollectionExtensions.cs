namespace TeamChat.AspNetCore;

public static class TeamChatServiceCollectionExtensions
{
    /// <summary>Adds the chat services, with the host's implementation of <see cref="IChatIdentityAdapter"/>.</summary>
    /// <remarks>The chat endpoints use the host's default authentication and authorization.</remarks>
    public static IServiceCollection AddTeamChat<TIdentityAdapter>(this IServiceCollection services, Action<TeamChatOptions> configure)
        where TIdentityAdapter : class, IChatIdentityAdapter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TeamChatOptions();
        configure(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RoutePrefix, "options.RoutePrefix");

        services.AddSingleton(options);
        services.AddScoped<IChatIdentityAdapter, TIdentityAdapter>();
        services.AddTeamChatApplication();
        services.AddTeamChatInfrastructure(new ChatStorageOptions(options.ConnectionString!, options.TablePrefix));

        return services;
    }
}
