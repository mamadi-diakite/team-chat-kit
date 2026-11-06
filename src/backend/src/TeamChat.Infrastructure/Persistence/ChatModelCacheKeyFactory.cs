namespace TeamChat.Infrastructure.Persistence;

/// <summary>EF Core caches one model per context type; the table prefix changes the model, so it is part of the key.</summary>
internal sealed class ChatModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), ((ChatDbContext)context).TablePrefix, designTime);
}
