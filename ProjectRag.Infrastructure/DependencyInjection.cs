using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectRag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddPersistence(configuration);
    }

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProjectRagDb")
            ?? throw new InvalidOperationException("Connection string 'ProjectRagDb' was not found.");

        services.AddDbContext<RagDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }
}
