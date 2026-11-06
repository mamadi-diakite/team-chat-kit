using Microsoft.Extensions.DependencyInjection;

namespace TeamChat.AspNetCore;

public static class TeamChatServiceCollectionExtensions
{
    public static IServiceCollection AddTeamChat(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
