using System.Collections.Generic;
using System.Linq;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Tzkt.Api.Swagger
{
    public class TzktExtensionProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            foreach (var param in context.OperationDescription.Operation.Parameters)
            {
                if (param.Schema.HasOneOfSchemaReference)
                {
                    var extensionData = param.Schema.OneOf.First().Reference.ExtensionData;
                    if (extensionData != null)
                    {
                        if (param.ExtensionData == null) 
                        {
                            param.ExtensionData = new Dictionary<string, object>();
                        }

                        foreach (var item in extensionData)
                        {
                            if (!param.ExtensionData.ContainsKey(item.Key)) 
                            {
                                param.ExtensionData.Add(item.Key, item.Value);
                            }
                        }
                    }
                }
            }
            return true;
        }
    }

    public class AnyOfExtensionProcessor : IOperationProcessor
    {
        private string OperationId { get; }
        private string AnyOfValues { get; }
        private const string ExtensionKey = "x-tzkt-extension";
        private const string AnyOfName = "anyof";
        private const string AnyOfExtension = "anyof-parameter";
        private const string AnyOfExtensionKey = "x-tzkt-anyof-parameter";

        public AnyOfExtensionProcessor(string operationId, string anyOfValues)
        {
            OperationId = operationId;
            AnyOfValues = anyOfValues;
        }

        public bool Process(OperationProcessorContext context)
        {
            if (context.OperationDescription.Operation.OperationId == OperationId) 
            {
                foreach (var param in context.OperationDescription.Operation.Parameters)
                {
                    if (param.Name == AnyOfName)
                    {
                        if (param.ExtensionData == null) 
                        {
                            param.ExtensionData = new Dictionary<string, object>();
                        }

                        param.ExtensionData.Add(ExtensionKey, AnyOfExtension);
                        param.ExtensionData.Add(AnyOfExtensionKey, AnyOfValues);
                    }
                }
            }
            return true;
        }
    }
}