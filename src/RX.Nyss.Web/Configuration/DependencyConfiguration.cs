﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.OpenApi.Models;
using RX.Nyss.Common.Configuration;
using RX.Nyss.Common.Utils.DataContract;
using RX.Nyss.Common.Utils.Logging;
using RX.Nyss.Data;
using RX.Nyss.Web.Data;
using RX.Nyss.Web.Features.Alerts.Access;
using RX.Nyss.Web.Features.Common;
using RX.Nyss.Web.Features.DataCollector.Access;
using RX.Nyss.Web.Features.DataConsumer.Access;
using RX.Nyss.Web.Features.Manager.Access;
using RX.Nyss.Web.Features.NationalSociety.Access;
using RX.Nyss.Web.Features.NationalSocietyStructure.Access;
using RX.Nyss.Web.Features.Project.Access;
using RX.Nyss.Web.Features.Report.Access;
using RX.Nyss.Web.Features.SmsGateway.Access;
using RX.Nyss.Web.Features.Supervisor.Access;
using RX.Nyss.Web.Features.TechnicalAdvisor.Access;
using RX.Nyss.Web.Features.User.Access;
using Serilog;

namespace RX.Nyss.Web.Configuration
{
    public static class DependencyConfiguration
    {
        public static void ConfigureDependencies(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var config = configuration.Get<ConfigSingleton>();
            RegisterLogger(serviceCollection, config.Logging, configuration);
            RegisterDatabases(serviceCollection, config.ConnectionStrings);
            RegisterAuth(serviceCollection, config.Authentication);
            RegisterWebFramework(serviceCollection);
            if (!config.IsProduction)
            {
                RegisterSwagger(serviceCollection);
            }

            RegisterServiceCollection(serviceCollection, config);
        }

        private static void RegisterLogger(IServiceCollection serviceCollection,
            ILoggingOptions loggingOptions, IConfiguration configuration)
        {
            const string applicationInsightsEnvironmentVariable = "APPINSIGHTS_INSTRUMENTATIONKEY";
            var appInsightsInstrumentationKey = configuration[applicationInsightsEnvironmentVariable];
            GlobalLoggerConfiguration.ConfigureLogger(loggingOptions, appInsightsInstrumentationKey);
            serviceCollection.AddSingleton(x => Log.Logger); // must be func, as the static logger is configured (changed reference) after DI registering
            serviceCollection.AddSingleton<ILoggerAdapter, SerilogLoggerAdapter>();

            if (!string.IsNullOrEmpty(appInsightsInstrumentationKey))
            {
                serviceCollection.AddApplicationInsightsTelemetry();
            }
        }

        private static void RegisterDatabases(IServiceCollection serviceCollection, IConnectionStringOptions connectionStringOptions)
        {
            serviceCollection.AddDbContext<NyssContext>(options =>
                options.UseSqlServer(connectionStringOptions.NyssDatabase,
                    x => x.UseNetTopologySuite()));

            serviceCollection.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionStringOptions.NyssDatabase));
        }

        private static void RegisterAuth(IServiceCollection serviceCollection, ConfigSingleton.AuthenticationOptions authenticationOptions)
        {
            serviceCollection.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // ToDo: The expiration should be this long only for verification, but shorter for password reset.
            serviceCollection.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromDays(10));

            serviceCollection.Configure<IdentityOptions>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
            });

            RegisterAuthorizationPolicies(serviceCollection);

            serviceCollection.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = authenticationOptions.CookieName;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(authenticationOptions.CookieExpirationTime);
                options.SlidingExpiration = true;

                options.Events.OnRedirectToLogin = context =>
                {
                    if (IsAjaxRequest(context.Request) || IsApiRequest(context.Request))
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (IsAjaxRequest(context.Request) || IsApiRequest(context.Request))
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            bool IsApiRequest(HttpRequest request) =>
                request.Path.StartsWithSegments("/api");

            bool IsAjaxRequest(HttpRequest request) =>
                string.Equals(request.Query["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal) ||
                string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);
        }

        private static void RegisterAuthorizationPolicies(IServiceCollection serviceCollection)
        {
            //ToDo: make some kind of automatic  registration of policies for all requirements in the assembly
            serviceCollection.AddAuthorization(options =>
            {
                options.AddPolicy(Policy.NationalSocietyAccess.ToString(),
                    policy => policy.Requirements.Add(new NationalSocietyAccessHandler.Requirement()));

                options.AddPolicy(Policy.ManagerAccess.ToString(),
                    policy => policy.Requirements.Add(new ManagerAccessHandler.Requirement()));

                options.AddPolicy(Policy.DataConsumerAccess.ToString(),
                    policy => policy.Requirements.Add(new DataConsumerAccessHandler.Requirement()));

                options.AddPolicy(Policy.TechnicalAdvisorAccess.ToString(),
                    policy => policy.Requirements.Add(new TechnicalAdvisorAccessHandler.Requirement()));

                options.AddPolicy(Policy.SmsGatewayAccess.ToString(),
                    policy => policy.Requirements.Add(new SmsGatewayAccessHandler.Requirement()));

                options.AddPolicy(Policy.SupervisorAccess.ToString(),
                    policy => policy.Requirements.Add(new SupervisorAccessHandler.Requirement()));

                options.AddPolicy(Policy.DataCollectorAccess.ToString(),
                    policy => policy.Requirements.Add(new DataCollectorAccessHandler.Requirement()));

                options.AddPolicy(Policy.ProjectAccess.ToString(),
                    policy => policy.Requirements.Add(new ProjectAccessHandler.Requirement()));

                options.AddPolicy(Policy.HeadManagerAccess.ToString(),
                    policy => policy.Requirements.Add(new HeadManagerAccessHandler.Requirement()));

                options.AddPolicy(Policy.RegionAccess.ToString(),
                    policy => policy.Requirements.Add(new RegionAccessHandler.Requirement()));

                options.AddPolicy(Policy.DistrictAccess.ToString(),
                    policy => policy.Requirements.Add(new DistrictAccessHandler.Requirement()));

                options.AddPolicy(Policy.VillageAccess.ToString(),
                    policy => policy.Requirements.Add(new VillageAccessHandler.Requirement()));

                options.AddPolicy(Policy.ZoneAccess.ToString(),
                    policy => policy.Requirements.Add(new ZoneAccessHandler.Requirement()));

                options.AddPolicy(Policy.AlertAccess.ToString(),
                    policy => policy.Requirements.Add(new AlertAccessHandler.Requirement()));

                options.AddPolicy(Policy.ReportAccess.ToString(),
                    policy => policy.Requirements.Add(new ReportAccessHandler.Requirement()));
            });

            serviceCollection.AddScoped<IAuthorizationHandler, NationalSocietyAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, ManagerAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, DataConsumerAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, TechnicalAdvisorAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, SmsGatewayAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, SupervisorAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, DataCollectorAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, HeadManagerAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, ProjectAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, RegionAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, DistrictAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, VillageAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, ZoneAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, AlertAccessHandler>();
            serviceCollection.AddScoped<IAuthorizationHandler, ReportAccessHandler>();
        }

        private static void RegisterWebFramework(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                })
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly()))
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = actionContext =>
                    {
                        var validationErrors = actionContext.ModelState.Where(v => v.Value.Errors.Count > 0)
                            .ToDictionary(stateEntry => stateEntry.Key,
                                stateEntry => stateEntry.Value.Errors.Select(x => x.ErrorMessage));

                        return new OkObjectResult(Result.Error(ResultKey.Validation.ValidationError, validationErrors));
                    };
                });

            serviceCollection.AddRazorPages();
            serviceCollection.AddHttpClient();

            // In production, the React files will be served from this directory
            serviceCollection.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });
        }

        private static void RegisterSwagger(IServiceCollection serviceCollection) =>
            serviceCollection.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nyss API", Version = "v1" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

        private static void RegisterServiceCollection(IServiceCollection serviceCollection, ConfigSingleton config)
        {
            serviceCollection.AddSingleton<IConfig>(config);
            serviceCollection.AddSingleton<INyssWebConfig>(config);
            RegisterTypes(serviceCollection, "RX.Nyss");
        }

        private static void RegisterTypes(IServiceCollection serviceCollection, string namePrefix) =>
            GetAssemblies(namePrefix: namePrefix)
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Select(type => new { implementationType = type, interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name == $"I{type.Name}") })
                .Where(x => x.interfaceType != null)
                .ToList()
                .ForEach(i => serviceCollection.AddScoped(i.interfaceType, i.implementationType));

        private static Assembly[] GetAssemblies(string namePrefix) =>
            DependencyContext.Default.GetDefaultAssemblyNames()
                .Where(name => name.Name.StartsWith(namePrefix))
                .Select(Assembly.Load)
                .ToArray();
    }
}
