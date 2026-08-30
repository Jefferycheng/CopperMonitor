using CopperMonitor.Application.Configs;
using CopperMonitor.Application.ExternalService;
using CopperMonitor.Application.Services;
using CopperMonitor.Infrastructure.ExternalServices;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Options
builder.Services.Configure<AlertOptions>(builder.Configuration.GetSection(AlertOptions.SectionName));
builder.Services.Configure<LineOptions>(builder.Configuration.GetSection(LineOptions.SectionName));

// MediatR — scan the Application assembly for command/query handlers.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CopperReportService).Assembly));

// Application services
builder.Services.AddScoped<CopperReportService>();
builder.Services.AddScoped<ChatCommandService>();
builder.Services.AddScoped<LineWebhookHandler>();

// External services
builder.Services.AddHttpClient<ICopperPriceProvider, YahooCopperPriceProvider>(ConfigureYahooClient);
builder.Services.AddHttpClient<IExchangeRateProvider, YahooExchangeRateProvider>(ConfigureYahooClient);
builder.Services.AddHttpClient<ILineMessenger, LineMessagingApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.line.me/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Build().Run();

// Yahoo's chart API rejects requests without a browser-like User-Agent.
static void ConfigureYahooClient(HttpClient client)
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)");
}
