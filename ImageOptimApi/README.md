# ImageOptimApi

This library provides access to the [ImageOptim.com API](https://imageoptim.com/api) from Microsoft .NET.

**This is a third party library and just consumes the ImageOptim API. It is not directly affiliated with ImageOptim.com.**

Issues found in this library should be filed at it's home: <https://github.com/MCLD/dotnet-imageoptim-api>

[ImageOptim.com](https://imageoptim.com/api) is a Web service for image compression and optimization. You will need to [register for a free trial username](https://imageoptim.com/api/register) in order to use this library.

See sample C# code using this NuGet package at: [MCLD/SampleImageOptimApi](https://github.com/MCLD/SampleImageOptimApi)

## Usage

### Summary

1. Install package with NuGet (`dotnet add ImageOptimApi` or `install-package ImageOptimApi`).
2. Prepare an environment where you can inject an `ILogger` and an `HttpClient`.
3. Create an `ImageOptimApi.Client()`
4. Configure the client object via properties, ensure you configure `Username`.
5. Execute `OptimizeAsync()`.

### ASP.NET Web application

Add the following packages to your project:

- `ImageOptimApi`

After the `builder` object is created, add the `ImageOptimApi.Client` to the `IServiceCollection`. You can set some global parameters here for the `HttpClient` that will make the Web calls:

- Please leave `AllowAutoRedirect`on per [ImageOptim API documentation](https://imageoptim.com/api/post)
- Configure the timeout here both for the `HttpClient` as well as what is passed into the ImageOptim API. A good default is 30 seconds.
- Please add a `UserAgent` header with the name of your product and the version.

```cs
    builder.Services.AddHttpClient<ImageOptimApi.Client>()
        .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            AllowAutoRedirect = true
        })
        .ConfigureHttpClient(_ =>
        {
            _.Timeout = TimeSpan.FromSeconds(30);
            _.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TestApp", "1.0.0"));
        });
```

In your MVC controller or Razor `PageModel`, add an `ImageOptimApi.Client` parameter to the constructor and store it in a local variable. Here's a Razor Pages example:

```cs
    private readonly ILogger<IndexModel> _logger;
    private readonly Client _client;

    public IndexModel(ILogger<IndexModel> logger, Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
```

You can then call `_client.OptimizeAsync()` in your methods as needed.

### Console application

Add the following packages to your project:

- `ImageOptimApi`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Http`
- `Microsoft.Extensions.Logging.Console`

In your application, configure a dependency injection `ServiceCollection` and `ServiceProvider`. You can set some global parameters here for the `HttpClient` that will make the Web calls:

- Please leave `AllowAutoRedirect`on per [ImageOptim API documentation](https://imageoptim.com/api/post)
- Configure the timeout here both for the `HttpClient` as well as what is passed into the ImageOptim API. A good default is 30 seconds.
- Please add a `UserAgent` header with the name of your product and the version.

```cs
    var services = new ServiceCollection();
    services.AddLogging(_ => _.AddConsole());
    services.AddHttpClient<Client>()
        .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            AllowAutoRedirect = true
        })
        .ConfigureHttpClient(_ =>
        {
            _.Timeout = TimeSpan.FromSeconds(30);
            _.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TestApp", "1.0.0"));
        });
    var provider = services.BuildServiceProvider();
```

When you need the `ImageOptimApi` client you can obtain it from the `ServiceProvider`:

```cs
    var client = provider.GetRequiredService<Client>();
    client.Username = "<username>";
```

You can then configure and use the client to optimize your images.

## License

The ImageOptimApi source code is Copyright 2022 by the Maricopa County Library District and is distributed under [The MIT License](http://opensource.org/licenses/MIT).
