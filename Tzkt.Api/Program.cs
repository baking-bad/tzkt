using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.Prometheus;
using Dapper;
using Tzkt.Api;
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

var builder = WebApplication.CreateBuilder(args);

#region configuration
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddEnvironmentVariables("TZKT_API_");
builder.Configuration.AddCommandLine(args);
#endregion

#region logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#endregion

#region services
builder.Services.AddDbContext<TzktContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<AccountsCache>();
builder.Services.AddSingleton<BigMapsCache>();
builder.Services.AddSingleton<AliasesCache>();
builder.Services.AddSingleton<ProtocolsCache>();
builder.Services.AddSingleton<QuotesCache>();
builder.Services.AddSingleton<SoftwareCache>();
builder.Services.AddSingleton<StateCache>();
builder.Services.AddSingleton<TimeCache>();
builder.Services.AddSingleton<ResponseCacheService>();

builder.Services.AddTransient<StateRepository>();
builder.Services.AddTransient<AccountRepository>();
builder.Services.AddTransient<OperationRepository>();
builder.Services.AddTransient<BalanceHistoryRepository>();
builder.Services.AddTransient<ReportRepository>();
builder.Services.AddTransient<BlockRepository>();
builder.Services.AddTransient<VotingRepository>();
builder.Services.AddTransient<ProtocolRepository>();
builder.Services.AddTransient<BakingRightsRepository>();
builder.Services.AddTransient<CyclesRepository>();
builder.Services.AddTransient<RewardsRepository>();
builder.Services.AddTransient<QuotesRepository>();
builder.Services.AddTransient<CommitmentRepository>();
builder.Services.AddTransient<StatisticsRepository>();
builder.Services.AddTransient<SoftwareRepository>();
builder.Services.AddTransient<BigMapsRepository>();
builder.Services.AddTransient<TokensRepository>();
builder.Services.AddTransient<MetadataRepository>();
builder.Services.AddTransient<ConstantsRepository>();
builder.Services.AddTransient<ContractEventsRepository>();
builder.Services.AddTransient<DomainsRepository>();

builder.Services.AddAuthService(builder.Configuration);
builder.Services.AddSingleton<RpcHelpers>();

builder.Services.AddHomeService();
builder.Services.AddStateListener();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new AccountConverter());
        options.JsonSerializerOptions.Converters.Add(new OperationConverter());
        options.JsonSerializerOptions.Converters.Add(new OperationErrorConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.MaxDepth = 100_000;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context => new BadRequest(context);
    });

builder.Services.AddOpenApiDocument();

if (builder.Configuration.GetWebsocketConfig().Enabled)
{
    builder.Services.AddTransient<HeadProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, HeadProcessor<DefaultHub>>();

    builder.Services.AddTransient<CyclesProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, CyclesProcessor<DefaultHub>>();

    builder.Services.AddTransient<BlocksProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, BlocksProcessor<DefaultHub>>();

    builder.Services.AddTransient<OperationsProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, OperationsProcessor<DefaultHub>>();

    builder.Services.AddTransient<BigMapsProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, BigMapsProcessor<DefaultHub>>();

    builder.Services.AddTransient<EventsProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, EventsProcessor<DefaultHub>>();

    builder.Services.AddTransient<TokenBalancesProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, TokenBalancesProcessor<DefaultHub>>();

    builder.Services.AddTransient<TokenTransfersProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, TokenTransfersProcessor<DefaultHub>>();

    builder.Services.AddTransient<AccountsProcessor<DefaultHub>>();
    builder.Services.AddTransient<IHubProcessor, AccountsProcessor<DefaultHub>>();

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
        options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    })
    .AddJsonProtocol(jsonOptions =>
    {
        jsonOptions.PayloadSerializerOptions.Converters.Add(new AccountConverter());
        jsonOptions.PayloadSerializerOptions.Converters.Add(new OperationConverter());
        jsonOptions.PayloadSerializerOptions.Converters.Add(new OperationErrorConverter());
        jsonOptions.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.PayloadSerializerOptions.MaxDepth = 100_000;
    });
}

if (builder.Configuration.GetHealthChecksConfig().Enabled)
{
    builder.Services.AddHealthChecks().AddCheck<DumbHealthCheck>(nameof(DumbHealthCheck));
}

builder.Services.AddMetrics(options =>
{
    options.Configuration.ReadFrom(builder.Configuration);
    options.OutputMetrics.AsPrometheusPlainText();
    options.OutputMetrics.AsPrometheusProtobuf();
});

builder.Services.AddMetricsEndpoints(builder.Configuration, options =>
{
    options.MetricsEndpointOutputFormatter = new MetricsPrometheusProtobufOutputFormatter();
    options.MetricsTextEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();
});

builder.Services.AddMetricsTrackingMiddleware(builder.Configuration, options =>
{
    options.OAuth2TrackingEnabled = false;
});
#endregion

#region dapper
SqlMapper.AddTypeHandler(new ProfileMetadataTypeHandler());
SqlMapper.AddTypeHandler(new JsonElementTypeHandler());
SqlMapper.AddTypeHandler(new RawJsonTypeHandler());
#endregion

var app = builder.Build();

#region init db
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TzktContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Version {version}", Assembly.GetExecutingAssembly().GetName().Version);

    while (true)
    {
        try
        {
            logger.LogInformation("Initialize database");

            var migrations = db.Database.GetMigrations().ToList();
            var applied = db.Database.GetAppliedMigrations().ToList();

            for (int i = 0; i < Math.Min(migrations.Count, applied.Count); i++)
            {
                if (migrations[i] != applied[i])
                {
                    throw new InvalidOperationException(
                        "API and DB have incompatible versions. Drop the DB and restore it from the appropriate snapshot.");
                }
            }

            if (applied.Count > migrations.Count)
            {
                throw new InvalidOperationException(
                    "API is out of date. Update the API to the newer version.");
            }

            if (applied.Count < migrations.Count)
            {
                logger.LogWarning("{cnt} pending migrations. Wait for the indexer to migrate the database...", migrations.Count - applied.Count);
                Thread.Sleep(1000);
                continue;
            }

            var state = db.AppState.Single();
            if (state.Level < 1)
            {
                logger.LogWarning("Database is empty, at least two blocks are needed");
                Thread.Sleep(1000);
                continue;
            }

            logger.LogInformation("Database initialized");
            break;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogCritical(ex, "Failed to initialize database. Exit...");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize database. Try again...");
            Thread.Sleep(1000);
        }
    }
}
#endregion

#region validate config
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.ValidateAuthConfig();
    await scope.ServiceProvider.ValidateRpcHelpersConfig();
}
#endregion

app.UseMetricsEndpoint();
app.UseMetricsTextEndpoint();

app.UseMetricsActiveRequestMiddleware();
app.UseMetricsApdexTrackingMiddleware();
app.UseMetricsErrorTrackingMiddleware();
app.UseMetricsRequestTrackingMiddleware();

app.UseCors(builder => builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()
    .WithExposedHeaders(
        StateHeadersMiddleware.TZKT_VERSION,
        StateHeadersMiddleware.TZKT_LEVEL,
        StateHeadersMiddleware.TZKT_KNOWN_LEVEL,
        StateHeadersMiddleware.TZKT_SYNCED_AT));

app.UseOpenApi();

app.UseMiddleware<StateHeadersMiddleware>();

app.MapControllers();

if (builder.Configuration.GetWebsocketConfig().Enabled)
{
    app.MapHub<DefaultHub>("/v1/ws");
    #region DEPRECATED
    app.MapHub<DefaultHub>("/v1/events");
    #endregion
}

if (builder.Configuration.GetHealthChecksConfig().Enabled)
{
    app.MapHealthChecks(builder.Configuration.GetHealthChecksConfig().Endpoint);
}

app.Run();
