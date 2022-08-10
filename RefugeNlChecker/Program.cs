using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RefugeNlChecker;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services
            .AddHostedService<CheckerService>();
        
        services
            .AddHttpClient<CheckerService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });
        services
            .AddHttpClient<ITelegramClient, TelegramClient>();
        
        services
            .AddScoped<ITelegramClient, TelegramClient>()
            .AddOptions<TelegramClientOptions>()
            .Bind(context.Configuration);
    })
    .RunConsoleAsync();
