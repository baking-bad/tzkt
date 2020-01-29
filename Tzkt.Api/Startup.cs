using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Metadata;
using Tzkt.Api.Services.Sync;

namespace Tzkt.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAccountMetadata();
            services.AddAccountsCache();
            services.AddStateCache();
            services.AddTimeCache();

            services.AddTransient<StateRepository>();
            services.AddTransient<AccountRepository>();
            services.AddTransient<OperationRepository>();
            services.AddTransient<ReportRepository>();
            services.AddTransient<BlockRepository>();
            services.AddTransient<VotingRepository>();
            services.AddTransient<ProtocolRepository>();

            services.AddSynchronization();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.Converters.Add(new AccountConverter());
                    options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
                    options.JsonSerializerOptions.Converters.Add(new OperationConverter());
                    options.JsonSerializerOptions.Converters.Add(new OperationErrorConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context => new BadRequest(context);
                });

            services.AddOpenApiDocument(options =>
            {
                options.DocumentName = "v1-preview";
                options.PostProcess = document =>
                {
                    document.Info.Title = "TzKT API";
                    document.Info.Description = "Early version of the TzKT API\n\nMainnet: `https://api.tzkt.io/`\n\nBabylonnet: `https://api.babylon.tzkt.io/`\n\nCarthagenet: `https://api.carthage.tzkt.io/`";
                    document.Info.Version = "v1-preview";
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Baking Bad",
                        Email = "hello@baking-bad.org",
                        Url = "https://baking-bad.org/docs"
                    };
                    document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        {
                            "x-logo", new
                            {
                                url = "https://tzkt.io/logo.png",
                                href = "https://tzkt.io/"
                            }
                        }
                    };
                    document.Produces = new[] { "application/json" };
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder.AllowAnyOrigin());

            app.UseOpenApi(options => 
            {
                options.Path = "/v1/swagger.json";
                options.DocumentName = "v1-preview";
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
