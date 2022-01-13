using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Utils;

namespace Tzkt.Api
{
    public class JsonBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext ctx)
        {
            var model = ctx.ModelName;
            JsonParameter res = null;

            foreach (var key in ctx.HttpContext.Request.Query.Keys)
            {
                if (key == model)
                {
                    if (!ctx.TryGetJson(key, out var val))
                        return Task.CompletedTask;
                    res ??= new();
                    res.Eq ??= new();
                    res.Eq.Add((Array.Empty<JsonPath>(), val));
                }
                else if (key.StartsWith($"{model}."))
                {
                    if (!JsonPath.TryParse(key[(model.Length + 1)..], out var path))
                    {
                        ctx.ModelState.AddModelError(key,
                            $"Path contains invalid item: {path.First(x => x.Type == JsonPathType.None).Value}");
                        return Task.CompletedTask;
                    }
                    if (path.Any(x => x.Type == JsonPathType.Any) && path.Any(x => x.Type == JsonPathType.Index))
                    {
                        ctx.ModelState.AddModelError(key,
                            $"Mixed array access is not allowed: [{path.First(x => x.Type == JsonPathType.Index).Value}] and [*]");
                        return Task.CompletedTask;
                    }

                    res ??= new();
                    switch (path[^1].Value)
                    {
                        case "eq":
                            if (!ctx.TryGetJson(key, out var eq))
                                return Task.CompletedTask;
                            res.Eq ??= new();
                            res.Eq.Add((path[..^1], eq));
                            break;
                        case "ne":
                            if (!ctx.TryGetJson(key, out var ne))
                                return Task.CompletedTask;
                            res.Ne ??= new();
                            res.Ne.Add((path[..^1], ne));
                            break;
                        case "gt":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetString(key, out var gt))
                                return Task.CompletedTask;
                            res.Gt ??= new();
                            res.Gt.Add((path[..^1], gt));
                            break;
                        case "ge":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetString(key, out var ge))
                                return Task.CompletedTask;
                            res.Ge ??= new();
                            res.Ge.Add((path[..^1], ge));
                            break;
                        case "lt":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetString(key, out var lt))
                                return Task.CompletedTask;
                            res.Lt ??= new();
                            res.Lt.Add((path[..^1], lt));
                            break;
                        case "le":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetString(key, out var le))
                                return Task.CompletedTask;
                            res.Le ??= new();
                            res.Le.Add((path[..^1], le));
                            break;
                        case "as":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetString(key, out var @as))
                                return Task.CompletedTask;
                            res.As ??= new();
                            res.As.Add((path[..^1], @as
                                .Replace("%", "\\%")
                                .Replace("\\*", "ъуъ")
                                .Replace("*", "%")
                                .Replace("ъуъ", "*")));
                            break;
                        case "un":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetString(key, out var un))
                                return Task.CompletedTask;
                            res.Un ??= new();
                            res.Un.Add((path[..^1], un
                                .Replace("%", "\\%")
                                .Replace("\\*", "ъуъ")
                                .Replace("*", "%")
                                .Replace("ъуъ", "*")));
                            break;
                        case "in":
                            if (!ctx.TryGetJsonArray(key, out var @in))
                                return Task.CompletedTask;
                            res.In ??= new();
                            res.In.Add((path[..^1], @in));
                            break;
                        case "ni":
                            if (!ctx.TryGetJsonArray(key, out var ni))
                                return Task.CompletedTask;
                            res.Ni ??= new();
                            res.Ni.Add((path[..^1], ni));
                            break;
                        case "null":
                            if (HasWildcard(ctx, key, path) || !ctx.TryGetBool(key, out var isNull))
                                return Task.CompletedTask;
                            res.Null ??= new();
                            res.Null.Add((path[..^1], (bool)isNull));
                            break;
                        default:
                            if (!ctx.TryGetJson(key, out var val))
                                return Task.CompletedTask;
                            res.Eq ??= new();
                            res.Eq.Add((path, val));
                            break;
                    }
                }
            }

            ctx.Result = ModelBindingResult.Success(res);
            return Task.CompletedTask;
        }

        static bool HasWildcard(ModelBindingContext ctx, string key, JsonPath[] path)
        {
            if (path.Any(x => x.Type == JsonPathType.Any))
            {
                ctx.ModelState.AddModelError(key, $"Path contains invalid item: [*] is not allowed in this mode");
                return true;
            }
            return false;
        }
    }
}
