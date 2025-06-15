namespace Tzkt.Api.Swagger
{
    public static class Swagger
    {
        const string Version = "1.15.1";
        const string Path = "/v1/swagger.json";

        public static void AddOpenApiDocument(this IServiceCollection services)
        {
            services.AddOpenApiDocument(options =>
            {
                options.DocumentName = Version;
                options.OperationProcessors.Add(new TzktExtensionProcessor());
                options.OperationProcessors.Add(new AnyOfExtensionProcessor("Tokens_GetTokenTransfers", "from,to"));
                options.OperationProcessors.Add(new AnyOfExtensionProcessor("Tokens_GetTokenTransfersCount", "from,to"));
                options.OperationProcessors.Add(new AnyOfExtensionProcessor("Tickets_GetTicketTransfers", "from,to"));
                options.OperationProcessors.Add(new AnyOfExtensionProcessor("Tickets_GetTicketTransfersCount", "from,to"));
                options.PostProcess = document =>
                {
                    document.Info.Title = "TzKT API";
                    document.Info.Version = Version;
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Baking Bad Team",
                        Email = "hello@bakingbad.dev",
                        Url = "https://bakingbad.dev"
                    };
                    document.Info.Description = File.Exists("Swagger/Description.md")
                        ? File.ReadAllText("Swagger/Description.md")
                        : null;
                    document.Info.ExtensionData = new Dictionary<string, object?>
                    {
                        { "x-logo", new { url = "https://tzkt.io/logo.png", href = "https://tzkt.io/" } }
                    };
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Get Started",
                        Description = File.Exists("Swagger/WsGetStarted.md")
                            ? File.ReadAllText("Swagger/WsGetStarted.md")
                            : null,
                        ExtensionData = new Dictionary<string, object?>{{"x-tagGroup", "ws"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Subscriptions",
                        Description = File.Exists("Swagger/WsSubscriptions.md")
                            ? File.ReadAllText("Swagger/WsSubscriptions.md")
                            : null,
                        ExtensionData = new Dictionary<string, object?>{{"x-tagGroup", "ws"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Examples",
                        Description = File.Exists("Swagger/WsExamples.md")
                            ? File.ReadAllText("Swagger/WsExamples.md")
                            : null,
                        ExtensionData = new Dictionary<string, object?>{{"x-tagGroup", "ws"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Typescript SDK",
                        Description = File.Exists("Swagger/TypescriptSdk.md")
                            ? File.ReadAllText("Swagger/TypescriptSdk.md")
                            : null,
                        ExtensionData = new Dictionary<string, object?>{{"x-tagGroup", "sdk"}}
                    });
                    document.Tags.Add(new NSwag.OpenApiTag
                    {
                        Name = "Taquito extension",
                        Description = File.Exists("Swagger/TaquitoExt.md")
                            ? File.ReadAllText("Swagger/TaquitoExt.md")
                            : null,
                        ExtensionData = new Dictionary<string, object?>{{"x-tagGroup", "sdk"}}
                    });
                    document.ExtensionData = new Dictionary<string, object?>
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
                                        .Where(x => x.ExtensionData?["x-tagGroup"]?.Equals("ws") == true)
                                        .Select(x => x.Name)
                                        .ToList()
                                },
                                new
                                {
                                    name = "Libraries",
                                    tags = document.Tags
                                        .Where(x => x.ExtensionData?["x-tagGroup"]?.Equals("sdk") == true)
                                        .Select(x => x.Name)
                                        .ToList()
                                }
                            }
                        }
                    };
                    document.Produces = ["application/json"];

                    // TODO: migrate to Microsoft.AspNetCore.OpenApi when .NET 10 released
                    foreach (var (type, schema) in document.Components.Schemas)
                    {
                        if (schema.AllOf.Count == 2 && schema.ResponsibleDiscriminatorObject?.PropertyName == "type")
                            schema.AllOf.Remove(schema.AllOf.First());

                        if (type == nameof(Models.Activity) && schema.ActualDiscriminatorObject != null)
                        {
                            var opSchema = document.Components.Schemas[nameof(Models.Operation)];
                            if (opSchema.ActualDiscriminatorObject != null)
                            {
                                foreach (var (k, v) in opSchema.ActualDiscriminatorObject!.Mapping)
                                    schema.ActualDiscriminatorObject.Mapping.TryAdd(k, v);
                            }
                        }
                    }
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
