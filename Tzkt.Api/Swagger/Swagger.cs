using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Api.Swagger
{
    public static class Swagger
    {
        const string Version = "v1.10.0";
        const string Path = "/v1/swagger.json";

        public static void AddOpenApiDocument(this IServiceCollection services)
        {
            services.AddOpenApiDocument(options =>
            {
                options.DocumentName = Version;
                options.OperationProcessors.Add(new TzktExtensionProcessor());
                options.OperationProcessors.Add(new AnyOfExtensionProcessor("Tokens_GetTokenTransfers", "from,to"));
                options.OperationProcessors.Add(new AnyOfExtensionProcessor("Tokens_GetTokenTransfersCount", "from,to"));
                options.PostProcess = document =>
                {
                    document.Info.Title = "TzKT API";
                    document.Info.Version = Version;
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Baking Bad Team",
                        Email = "hello@baking-bad.org",
                        Url = "https://baking-bad.org/docs"
                    };
                    document.Info.Description = File.Exists("Swagger/Description.md")
                        ? File.ReadAllText("Swagger/Description.md")
                        : null;
                    document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        { "x-logo", new { url = "https://tzkt.io/logo.png", href = "https://tzkt.io/" } }
                    };
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Get Started",
                        Description = File.Exists("Swagger/WsGetStarted.md")
                            ? File.ReadAllText("Swagger/WsGetStarted.md")
                            : null,
                        ExtensionData = new Dictionary<string, object>{{"x-tagGroup", "ws"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Subscriptions",
                        Description = File.Exists("Swagger/WsSubscriptions.md")
                            ? File.ReadAllText("Swagger/WsSubscriptions.md")
                            : null,
                        ExtensionData = new Dictionary<string, object>{{"x-tagGroup", "ws"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Examples",
                        Description = File.Exists("Swagger/WsExamples.md")
                            ? File.ReadAllText("Swagger/WsExamples.md")
                            : null,
                        ExtensionData = new Dictionary<string, object>{{"x-tagGroup", "ws"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Typescript SDK",
                        Description = File.Exists("Swagger/TypescriptSdk.md")
                            ? File.ReadAllText("Swagger/TypescriptSdk.md")
                            : null,
                        ExtensionData = new Dictionary<string, object>{{"x-tagGroup", "sdk"}}
                    });
                    document.ExtensionData = new Dictionary<string, object>
                    {
                        {
                            "x-tagGroups", new []
                            {
                                new
                                {
                                    name = "REST API",
                                    tags = document.Operations.Select(x => x.Operation.Tags[0]).Distinct().ToList()
                                },
                                new
                                {
                                    name = "WebSocket API",
                                    tags = document.Tags
                                        .Where(x => x.ExtensionData["x-tagGroup"].Equals("ws"))
                                        .Select(x => x.Name)
                                        .ToList()
                                },
                                new
                                {
                                    name = "Libraries",
                                    tags = document.Tags
                                        .Where(x => x.ExtensionData["x-tagGroup"].Equals("sdk"))
                                        .Select(x => x.Name)
                                        .ToList()
                                }
                            }
                        }
                    };
                    document.Produces = new[] { "application/json" };
                };
            });
        }

        public static void UseOpenApi(this IApplicationBuilder app)
        {
            app.UseOpenApi(options =>
            {
                options.Path = Path;
                options.DocumentName = Version;
            });
        }
    }
}
