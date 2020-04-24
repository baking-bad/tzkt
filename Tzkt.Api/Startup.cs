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
            services.AddProposalMetadata();
            services.AddProtocolMetadata();

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
            services.AddTransient<BakingRightsRepository>();

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
                    document.Info.Description = "TzKT Explorer provides an API for accessing the extended Tezos blockchain data. This API is free for both commercial and non-commercial usage. " +
                        "Also, TzKT API is an open-source project, so you can clone and build it and use as a self-hosted service to avoid risks of using third-party services. " +
                        "Feel free to contact us if you need specific features or endpoints or query parameters, etc.\n\n" +
                        "TzKT API is available for the following Tezos networks:\n\n" +
                        "Mainnet: `https://api.tzkt.io/`\n\n" + 
                        "Babylonnet: `https://api.babylon.tzkt.io/`\n\n" +
                        "Carthagenet: `https://api.carthage.tzkt.io/`\n\n" +
                        "Zeronet: `https://api.zeronet.tzkt.io/`";
                    document.Info.Version = "v1-preview";
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Baking Bad Team",
                        Email = "hello@baking-bad.org",
                        Url = "https://tzkt.io"
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
