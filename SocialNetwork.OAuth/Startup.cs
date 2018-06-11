using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialNetwork.OAuth.Configuration;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using SocialNetwork.Data.Repositories;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.Google;
using IdentityServer4;

namespace SocialNetwork.OAuth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IUserValidator, UserValidator>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddSingleton<Func<IDbConnection>>(() => new SqlConnection(Configuration.GetConnectionString("SocialNetwork")));

            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
            });

            var assembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddIdentityServer()
                .AddSigningCredential(new X509Certificate2(@"D:\Research\SocialNetwork\socialnetwork.pfx", "password"))
                .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
                .AddConfigurationStore(options =>
                 {
                     options.ConfigureDbContext = builder => 
                     builder.UseSqlServer(Configuration.GetConnectionString("SocialNetwork.OAuth"), dbOptions =>
                                          dbOptions.MigrationsAssembly(assembly));
                 })
                 .AddOperationalStore(options =>
                 {
                     options.ConfigureDbContext = builder =>
                     builder.UseSqlServer(Configuration.GetConnectionString("SocialNetwork.OAuth"), dbOptions =>
                                          dbOptions.MigrationsAssembly(assembly));
                 });

            //.AddTestUsers(InMemoryConfiguration.Users().ToList()
            //.AddInMemoryClients(InMemoryConfiguration.Clients())
            //.AddInMemoryIdentityResources(InMemoryConfiguration.IdentityResources())
            //.AddInMemoryApiResources(InMemoryConfiguration.ApiResources());

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            MigrateInMemoryDataToSqlServer(app);

            loggerFactory.AddConsole();

            app.UseDeveloperExceptionPage();

            app.UseIdentityServer();

            app.UseStaticFiles();

            app.UseMvcWithDefaultRoute();
        }

        public void MigrateInMemoryDataToSqlServer(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                context.Database.Migrate();

                if (!context.Clients.Any())
                {
                    foreach (var client in InMemoryConfiguration.Clients())
                    {
                        context.Clients.Add(client.ToEntity());
                    }

                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in InMemoryConfiguration.IdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in InMemoryConfiguration.ApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }
            }
        }
    }
}
