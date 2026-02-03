using Microsoft.AspNetCore.SignalR;

namespace WateringController.Backend.Hubs;

/// <summary>
/// SignalR hub used to broadcast watering system updates to clients.
/// </summary>
public sealed class WateringHub : Hub
{
}
