using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using WateringController.Frontend;
using WateringController.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<WateringHubClient>();
builder.Services.AddScoped<FrontendTelemetryService>();

var host = builder.Build();
var js = host.Services.GetRequiredService<IJSRuntime>();

await js.InvokeVoidAsync("WateringTelemetry.start", new
{
    enabled = builder.Configuration.GetValue<bool>("OpenTelemetry:Enabled"),
    useBrowserSdk = builder.Configuration.GetValue<bool>("OpenTelemetry:UseBrowserSdk"),
    serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "WateringController.Frontend",
    otlpHttpEndpoint = builder.Configuration["OpenTelemetry:OtlpHttpEndpoint"] ?? "http://localhost:4318",
    fallbackEndpoint = builder.Configuration["OpenTelemetry:FallbackEndpoint"] ?? "/api/otel/client-event"
});

await host.RunAsync();
