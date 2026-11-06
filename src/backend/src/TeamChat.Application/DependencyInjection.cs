namespace TeamChat.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamChatApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<CurrentChatUserResolver>();

        return services;
    }
}
