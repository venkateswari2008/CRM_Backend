using System.Reflection;
using CRM.Application.Auth.Services;
using CRM.Application.Customers.Services;
using CRM.Application.Dashboard.Services;
using CRM.Application.Sales.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
