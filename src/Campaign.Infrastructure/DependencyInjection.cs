using Campaign.Application.Common;
using Campaign.Application.Customers;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Soap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Campaign.Application.Purchases;
using Campaign.Infrastructure.Csv;

namespace Campaign.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CampaignDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));
        services.AddScoped<IPurchaseCsvParser, PurchaseCsvParser>();

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<CampaignDbContext>());

        var directoryUrl = configuration["CustomerDirectory:Url"]
            ?? throw new InvalidOperationException("Missing configuration value 'CustomerDirectory:Url'.");

        services.AddHttpClient<ICustomerDirectory, SoapDemoCustomerDirectory>(client =>
        {
            client.BaseAddress = new Uri(directoryUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}