using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class JsonBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            JsonParameter res = null;

            foreach (var key in bindingContext.HttpContext.Request.Query.Keys.Where(x => x == model || x.StartsWith($"{model}.")))
            {
                var arr = key.Split(".", StringSplitOptions.RemoveEmptyEntries);
                var hasValue = false;

                switch (arr[^1])
                {
                    case "eq":
                        hasValue = false;
                        if (!bindingContext.TryGetString(key, ref hasValue, out var eq))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.Eq ??= new List<(string, string)>();
                            res.Eq.Add((string.Join(',', arr[1..^1]), eq));
                        }
                        break;
                    case "ne":
                        hasValue = false;
                        if (!bindingContext.TryGetString(key, ref hasValue, out var ne))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.Ne ??= new List<(string, string)>();
                            res.Ne.Add((string.Join(',', arr[1..^1]), ne));
                        }
                        break;
                    case "as":
                        hasValue = false;
                        if (!bindingContext.TryGetString(key, ref hasValue, out var @as))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.As ??= new List<(string, string)>();
                            res.As.Add((string.Join(',', arr[1..^1]), @as
                                .Replace("%", "\\%")
                                .Replace("\\*", "ъуъ")
                                .Replace("*", "%")
                                .Replace("ъуъ", "*")));
                        }
                        break;
                    case "un":
                        hasValue = false;
                        if (!bindingContext.TryGetString(key, ref hasValue, out var un))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.Un ??= new List<(string, string)>();
                            res.Un.Add((string.Join(',', arr[1..^1]), un
                                .Replace("%", "\\%")
                                .Replace("\\*", "ъуъ")
                                .Replace("*", "%")
                                .Replace("ъуъ", "*")));
                        }
                        break;
                    case "in":
                        hasValue = false;
                        if (!bindingContext.TryGetStringList(key, ref hasValue, out var @in))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.In ??= new List<(string, List<string>)>();
                            res.In.Add((string.Join(',', arr[1..^1]), @in));
                        }
                        break;
                    case "ni":
                        hasValue = false;
                        if (!bindingContext.TryGetStringList(key, ref hasValue, out var ni))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.Ni ??= new List<(string, List<string>)>();
                            res.Ni.Add((string.Join(',', arr[1..^1]), ni));
                        }
                        break;
                    case "null":
                        hasValue = false;
                        if (!bindingContext.TryGetBool(key, ref hasValue, out var isNull))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.Null ??= new List<(string, bool)>();
                            res.Null.Add((string.Join(',', arr[1..^1]), (bool)isNull));
                        }
                        break;
                    default:
                        hasValue = false;
                        if (!bindingContext.TryGetString(key, ref hasValue, out var value))
                            return Task.CompletedTask;
                        if (hasValue)
                        {
                            res ??= new JsonParameter();
                            res.Eq ??= new List<(string, string)>();
                            res.Eq.Add((string.Join(',', arr[1..]), value));
                        }
                        break;
                }
            }

            bindingContext.Result = ModelBindingResult.Success(res);
            return Task.CompletedTask;
        }
    }
}
