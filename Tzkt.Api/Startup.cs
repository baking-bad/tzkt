using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Metadata;
using Tzkt.Api.Services.Sync;
using Tzkt.Data;

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
            services.AddDbContext<TzktContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddAccountMetadata();
            services.AddProposalMetadata();
            services.AddProtocolMetadata();

            services.AddAccountsCache();
            services.AddProtocolsCache();
            services.AddQuotesCache();
            services.AddStateCache();
            services.AddTimeCache();

            services.AddTransient<StateRepository>();
            services.AddTransient<AccountRepository>();
            services.AddTransient<OperationRepository>();
            services.AddTransient<BalanceHistoryRepository>();
            services.AddTransient<ReportRepository>();
            services.AddTransient<BlockRepository>();
            services.AddTransient<VotingRepository>();
            services.AddTransient<ProtocolRepository>();
            services.AddTransient<BakingRightsRepository>();
            services.AddTransient<CyclesRepository>();
            services.AddTransient<RewardsRepository>(); 
            services.AddTransient<QuotesRepository>();

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
                options.DocumentName = "v1.1";
                options.PostProcess = document =>
                {
                    document.Info.Title = "TzKT API";

                    if (File.Exists("Description.md"))
                        document.Info.Description = File.ReadAllText("Description.md");

                    document.Info.Version = "v1.1";
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Baking Bad Team",
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
                options.DocumentName = "v1.1";
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
