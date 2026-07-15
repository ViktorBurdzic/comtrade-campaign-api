using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campaign.Application.Purchases;
using Campaign.Application.Rewards;
using Microsoft.Extensions.DependencyInjection;
    using Campaign.Application.Reports;

namespace Campaign.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<IPurchaseImportService, PurchaseImportService>();
        return services;
    }
}