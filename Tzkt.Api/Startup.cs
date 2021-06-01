using System;
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
using Tzkt.Api.Services;
using Tzkt.Api.Swagger;
using Tzkt.Api.Websocket;
using Tzkt.Api.Websocket.Hubs;
using Tzkt.Api.Websocket.Processors;
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
            services.AddSoftwareMetadata();

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
            services.AddTransient<CommitmentRepository>();
            services.AddTransient<StatisticsRepository>();
            services.AddTransient<SoftwareRepository>();
            services.AddTransient<BigMapsRepository>();

            services.AddHomeService();
            services.AddStateListener();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.MaxDepth = 1024;
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

            services.AddOpenApiDocument();

            #region websocket
            if (Configuration.GetWebsocketConfig().Enabled)
            {
                services.AddTransient<HeadProcessor<DefaultHub>>();
                services.AddTransient<IHubProcessor, HeadProcessor<DefaultHub>>();

                services.AddTransient<BlocksProcessor<DefaultHub>>();
                services.AddTransient<IHubProcessor, BlocksProcessor<DefaultHub>>();

                services.AddTransient<OperationsProcessor<DefaultHub>>();
                services.AddTransient<IHubProcessor, OperationsProcessor<DefaultHub>>();

                services.AddTransient<BigMapsProcessor<DefaultHub>>();
                services.AddTransient<IHubProcessor, BigMapsProcessor<DefaultHub>>();

                services.AddSignalR(options =>
                    {
                        options.EnableDetailedErrors = true;
                        options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                    })
                    .AddJsonProtocol(jsonOptions =>
                    {
                        jsonOptions.PayloadSerializerOptions.MaxDepth = 1024;
                        jsonOptions.PayloadSerializerOptions.IgnoreNullValues = true;
                        jsonOptions.PayloadSerializerOptions.Converters.Add(new AccountConverter());
                        jsonOptions.PayloadSerializerOptions.Converters.Add(new DateTimeConverter());
                        jsonOptions.PayloadSerializerOptions.Converters.Add(new OperationConverter());
                        jsonOptions.PayloadSerializerOptions.Converters.Add(new OperationErrorConverter());
                    });
            }
            #endregion
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials());

            app.UseOpenApi();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                if (Configuration.GetWebsocketConfig().Enabled)
                {
                    endpoints.MapHub<DefaultHub>("/v1/events");
                }
            });
        }
    }
}