using CopperMonitor.Api.Apis;
using CopperMonitor.Api.BackgroundServices;
using CopperMonitor.Api.Middlewares;
using CopperMonitor.Application.Configs;
using CopperMonitor.Application.ExternalService;
using CopperMonitor.Application.Services;
using CopperMonitor.Infrastructure.ExternalServices;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<AlertOptions>(builder.Configuration.GetSection(AlertOptions.SectionName));
builder.Services.Configure<LineOptions>(builder.Configuration.GetSection(LineOptions.SectionName));

// MediatR — scan the Application assembly for command/query handlers.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CopperReportService).Assembly));

// Application services
builder.Services.AddScoped<CopperReportService>();
builder.Services.AddScoped<ChatCommandService>();

// External services
builder.Services.AddHttpClient<ICopperPriceProvider, YahooCopperPriceProvider>(ConfigureYahooClient);
builder.Services.AddHttpClient<IExchangeRateProvider, YahooExchangeRateProvider>(ConfigureYahooClient);
builder.Services.AddHttpClient<ILineMessenger, LineMessagingApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.line.me/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Scheduler: weekdays 08:30 Asia/Taipei by default (see ReportSchedule in appsettings.json).
builder.Services.AddHostedService<DailyCopperReportService>();

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();
app.MapCopperPriceApiV1();
app.MapLineWebhookApiV1();
app.MapGet("/", () => "CopperMonitor is running.");

app.Run();

// Yahoo's chart API rejects requests without a browser-like User-Agent.
static void ConfigureYahooClient(HttpClient client)
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)");
}
