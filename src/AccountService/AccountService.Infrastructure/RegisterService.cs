using AccountService.Application.Interfaces;
using AccountService.Infrastructure.Data;
using AccountService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountService.Infrastructure;

public static class RegisterService
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AccountDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("AccountDb") ?? "Data Source=accountservice.db"));

        services.AddScoped<IAccountRepository, AccountRepository>();

        return services;
    }
}

