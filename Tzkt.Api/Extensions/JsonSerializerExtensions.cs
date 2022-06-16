using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api.Extensions
{
    public static class JsonSerializerExtensions
    {
        public static JsonOptions ConfigureJsonOptions(this JsonOptions options)
        {
            options.JsonSerializerOptions.MaxDepth = 100_000;
            options.JsonSerializerOptions.IgnoreNullValues = true;
            options.JsonSerializerOptions.Converters.Add(new AccountConverter());
            options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new OperationConverter());
            options.JsonSerializerOptions.Converters.Add(new OperationErrorConverter());
            return options;
        }
    }
}