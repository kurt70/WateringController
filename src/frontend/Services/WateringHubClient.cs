using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using WateringController.Frontend.Models;

namespace WateringController.Frontend.Services;

public sealed class WateringHubClient : IAsyncDisposable
{
    private readonly NavigationManager _navigation;
    private HubConnection? _connection;

    public WateringHubClient(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    public event Action<WaterLevelUpdate>? WaterLevelUpdated;
    public event Action<PumpStateUpdate>? PumpStateUpdated;
    public event Action<SystemAlarmUpdate>? AlarmRaised;
    public event Action<bool>? ConnectionStateChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (_connection is null)
        {
            var hubUri = _navigation.ToAbsoluteUri("/hubs/watering");
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUri)
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnected += _ =>
            {
                ConnectionStateChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            _connection.Reconnecting += _ =>
            {
                ConnectionStateChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            _connection.Closed += _ =>
            {
                ConnectionStateChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            _connection.On<WaterLevelUpdate>("WaterLevelUpdated", update =>
            {
                WaterLevelUpdated?.Invoke(update);
            });

            _connection.On<PumpStateUpdate>("PumpStateUpdated", update =>
            {
                PumpStateUpdated?.Invoke(update);
            });

            _connection.On<SystemAlarmUpdate>("AlarmRaised", update =>
            {
                AlarmRaised?.Invoke(update);
            });
        }

        if (_connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

        await _connection.StartAsync();
        ConnectionStateChanged?.Invoke(true);
    }

    public async Task StopAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
