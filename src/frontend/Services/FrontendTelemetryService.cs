using Microsoft.JSInterop;

namespace WateringController.Frontend.Services;

public sealed class FrontendTelemetryService
{
    private readonly IJSRuntime _jsRuntime;

    public FrontendTelemetryService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task TrackEventAsync(string eventName, Dictionary<string, object?>? attributes = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("WateringTelemetry.trackEvent", eventName, attributes ?? new Dictionary<string, object?>());
        }
        catch
        {
            // Telemetry should never affect UI flow.
        }
    }
}
