namespace TeamChat.AspNetCore;

public static class TeamChatServiceCollectionExtensions
{
    public static IServiceCollection AddTeamChat(this IServiceCollection services, Action<TeamChatOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TeamChatOptions();
        configure(options);

        return services.AddTeamChatInfrastructure(new ChatStorageOptions(options.ConnectionString!, options.TablePrefix));
    }
}
