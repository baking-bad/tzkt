using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Tzkt.Api.Tests.Parameters;

public class BindersTests
{
    static DefaultModelBindingContext CreateContext(string modelName, string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(queryString);

        return new DefaultModelBindingContext
        {
            ActionContext = new ActionContext { HttpContext = httpContext },
            ModelName = modelName,
            ModelState = new ModelStateDictionary(),
            ValueProvider = new QueryStringValueProvider(BindingSource.Query, httpContext.Request.Query, CultureInfo.InvariantCulture),
        };
    }

    #region string parameter
    [Theory]
    [InlineData("?path=%00")]
    [InlineData("?path.in=abc,%00")]
    public async Task StringBinder_RejectsNul(string query)
    {
        var ctx = CreateContext("path", query);
        await new StringBinder().BindModelAsync(ctx);

        Assert.False(ctx.Result.IsModelSet);
        Assert.False(ctx.ModelState.IsValid);
    }

    [Fact]
    public async Task StringBinder_AcceptsRegularValues()
    {
        var ctx = CreateContext("path", "?path=ledger&path.in=a,b&path.as=*ledger");
        await new StringBinder().BindModelAsync(ctx);

        Assert.True(ctx.ModelState.IsValid);
        var model = Assert.IsType<StringParameter>(ctx.Result.Model);
        Assert.Equal("ledger", model.Eq);
        Assert.Equal(new[] { "a", "b" }, model.In);
        Assert.Equal("%ledger", model.As);
    }
    #endregion

    #region json parameter
    [Theory]
    [InlineData("?value=%7B%22a%22%3A%22%5Cu0000%22%7D")]        // {"a":"\u0000"}
    [InlineData("?value.a=%5Cu0000")]                            // value.a=\u0000 (wrapped into a JSON string)
    [InlineData("?value.a.gt=%00")]
    [InlineData("?value.a.in=%5B%22x%22%2C%22%5Cu0000%22%5D")]   // ["x","\u0000"]
    [InlineData("?value.%22a%00b%22=1")]                         // raw NUL inside a quoted path key
    public async Task JsonBinder_RejectsInvalidCharacters(string query)
    {
        var ctx = CreateContext("value", query);
        await new JsonBinder().BindModelAsync(ctx);

        Assert.False(ctx.Result.IsModelSet);
        Assert.False(ctx.ModelState.IsValid);
    }

    [Fact]
    public async Task JsonBinder_AcceptsRegularValues()
    {
        var ctx = CreateContext("value", "?value.a=1&value.%22b%20c%22.gt=2&value.d.in=x,y&value.e.as=*z*&value.f=%22%5Cud83d%5Cude00%22");
        await new JsonBinder().BindModelAsync(ctx);

        Assert.True(ctx.ModelState.IsValid);
        var model = Assert.IsType<JsonParameter>(ctx.Result.Model);
        Assert.Equal(2, model.Eq!.Count);
        Assert.Single(model.Gt!);
        Assert.Single(model.In!);
        Assert.Single(model.As!);
    }
    #endregion
}
