using Biblioteca.Web.Data;
using Biblioteca.Web.Data.Repositories;
using Biblioteca.Web.Data.UnitOfWork;
using Biblioteca.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando aplicação Biblioteca Áurea");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllersWithViews();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/admin/login";
            options.LogoutPath = "/admin/logout";
            options.AccessDeniedPath = "/admin/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

    builder.Services.AddAuthorization();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "A connection string 'DefaultConnection' não foi encontrada. Configure em appsettings.json, appsettings.Development.json ou User Secrets.");
    }

    var usarSqlite =
        connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) ||
        connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

    builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    {
        if (usarSqlite)
        {
            options.UseSqlite(connectionString);
        }
        else
        {
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);

                    sqlOptions.CommandTimeout(60);
                });
        }
    });

    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IEmprestimoAppService, EmprestimoAppService>();

    var app = builder.Build();

    if (usarSqlite)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();

        context.Database.EnsureCreated();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";
    });

    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (HostAbortedException)
{
    // Ignorado durante comandos do Entity Framework.
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação Biblioteca Áurea foi encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}