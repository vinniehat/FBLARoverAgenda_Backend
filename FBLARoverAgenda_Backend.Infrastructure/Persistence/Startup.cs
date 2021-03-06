using Audit.Core;
using FBLARoverAgenda_Backend.Domain.Entities.Audit;
using FBLARoverAgenda_Backend.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace FBLARoverAgenda_Backend.Infrastructure.Persistence;

public static class Startup
{
	/// <summary>
	///     Adds all Entity Framework database contexts to the service collection
	/// </summary>
	public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("AppContext");
		services.AddDbContext<ApplicationDbContext>(options =>
		{
			options.UseSqlServer(connectionString,
				x => x.MigrationsAssembly("FBLARoverAgenda_Backend.Infrastructure"));
		}, optionsLifetime: ServiceLifetime.Singleton, contextLifetime: ServiceLifetime.Scoped);

		services.AddDbContextFactory<ApplicationDbContext>(options =>
		{
			options.UseSqlServer(connectionString,
				x => x.MigrationsAssembly("FBLARoverAgenda_Backend.Infrastructure"));
		});

		Audit.EntityFramework.Configuration.Setup()
			.ForContext<ApplicationDbContext>(config => config
				.IncludeEntityObjects()
				.AuditEventType("{context}:{database}"))
			.UseOptIn();

		Audit.Core.Configuration.Setup()
			.UseEntityFramework(_ => _
				.AuditTypeMapper(t => typeof(AuditLog))
				.AuditEntityAction<AuditLog>((ev, entry, entity) =>
				{
					entity.TableName = entry.Table;
					entity.AuditData = entry.ToJson();
					entity.EntityType = entry.EntityType.Name;
					entity.AuditAction = entry.Action;
					entity.AuditDate = DateTime.UtcNow;
					entity.AuditUser = ev.Environment.UserName; // (string)(ev.Environment.CustomFields.ContainsKey("IdentityUserName") ? ev.Environment.CustomFields["IdentityUserName"] : Environment.UserName);
					entity.TablePK = entry.PrimaryKey.First().Value.ToString();
				})
				.IgnoreMatchedProperties(true));
	}

	public static void Configure(IApplicationBuilder app, IConfiguration configuration)
	{
		Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
		{
			using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
			{
				var httpContextAccessor = serviceScope.ServiceProvider.GetService<IHttpContextAccessor>();
				var httpContext = httpContextAccessor?.HttpContext;

				scope.Event.Environment.UserName = Environment.UserName ?? "";
				if (httpContext?.User?.Identity != null)
				{
					scope.Event.Environment.UserName = httpContext.User.Identity.Name;
				}
			}

		});
	}
}

