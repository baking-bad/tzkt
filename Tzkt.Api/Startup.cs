using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dapper;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Auth;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Sync;
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

            services.AddSingleton<AccountsCache>();
            services.AddSingleton<BigMapsCache>();
            services.AddSingleton<AliasesCache>();
            services.AddSingleton<ProtocolsCache>();
            services.AddSingleton<QuotesCache>();
            services.AddSingleton<SoftwareCache>();
            services.AddSingleton<StateCache>();
            services.AddSingleton<TimeCache>();

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
            services.AddTransient<MetadataRepository>();

            services.AddAuthService(Configuration);

            services.AddHomeService();
            services.AddStateListener();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.MaxDepth = 100_000;
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

                services.AddTransient<AccountsProcessor<DefaultHub>>();
                services.AddTransient<IHubProcessor, AccountsProcessor<DefaultHub>>();

                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                })
                .AddJsonProtocol(jsonOptions =>
                {
                    jsonOptions.PayloadSerializerOptions.MaxDepth = 100_000;
                    jsonOptions.PayloadSerializerOptions.IgnoreNullValues = true;
                    jsonOptions.PayloadSerializerOptions.Converters.Add(new AccountConverter());
                    jsonOptions.PayloadSerializerOptions.Converters.Add(new DateTimeConverter());
                    jsonOptions.PayloadSerializerOptions.Converters.Add(new OperationConverter());
                    jsonOptions.PayloadSerializerOptions.Converters.Add(new OperationErrorConverter());
                });
            }
            #endregion

            #region health checks
            var healthChecks = Configuration.GetHealthChecksConfig();
            if (healthChecks.Enabled)
            {
                services.AddHealthChecks()
                    .AddCheck<DumbHealthCheck>(nameof(DumbHealthCheck));
            }
            #endregion

            #region dapper
            SqlMapper.AddTypeHandler(new AccountMetadataTypeHandler());
            SqlMapper.AddTypeHandler(new JsonElementTypeHandler());
            SqlMapper.AddTypeHandler(new RawJsonTypeHandler());
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

                #region web socket
                if (Configuration.GetWebsocketConfig().Enabled)
                {
                    endpoints.MapHub<DefaultHub>("/v1/events");
                }
                #endregion

                #region health checks
                var healthChecks = Configuration.GetHealthChecksConfig();
                if (healthChecks.Enabled)
                {
                    endpoints.MapHealthChecks(healthChecks.Endpoint);
                }
                #endregion
            });
        }
    }
}