using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace WateringController.Backend.Mqtt;

/// <summary>
/// Manages the MQTT connection lifecycle and routes inbound messages to handlers.
/// </summary>
public sealed class MqttClientHostedService : BackgroundService, IMqttPublisher
{
    private readonly MqttOptions _options;
    private readonly MqttTopics _topics;
    private readonly WaterLevelMqttHandler _waterLevelHandler;
    private readonly PumpStateMqttHandler _pumpStateHandler;
    private readonly SystemAlarmMqttHandler _alarmHandler;
    private readonly MqttConnectionState _connectionState;
    private readonly ILogger<MqttClientHostedService> _logger;
    private readonly IMqttClient _client;
    private bool _subscribed;

    public MqttClientHostedService(
        IOptions<MqttOptions> options,
        MqttTopics topics,
        WaterLevelMqttHandler waterLevelHandler,
        PumpStateMqttHandler pumpStateHandler,
        SystemAlarmMqttHandler alarmHandler,
        MqttConnectionState connectionState,
        ILogger<MqttClientHostedService> logger)
    {
        _options = options.Value;
        _topics = topics;
        _waterLevelHandler = waterLevelHandler;
        _pumpStateHandler = pumpStateHandler;
        _alarmHandler = alarmHandler;
        _connectionState = connectionState;
        _logger = logger;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
    }

    public bool IsConnected => _client.IsConnected;

    /// <summary>
    /// Publishes a message if connected, otherwise logs and drops the message.
    /// </summary>
    public async Task PublishAsync(string topic, string payload, bool retain, CancellationToken cancellationToken)
    {
        if (!_client.IsConnected)
        {
            _logger.LogWarning("MQTT publish skipped (disconnected). Topic {Topic}.", topic);
            return;
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(retain)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
    }

    /// <summary>
    /// Background loop that connects, subscribes, and dispatches incoming messages.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.ConnectedAsync += _ =>
        {
            _subscribed = false;
            _connectionState.MarkConnected(DateTimeOffset.UtcNow);
            _logger.LogInformation("Connected to MQTT broker {Host}:{Port}.", _options.Host, _options.Port);
            return Task.CompletedTask;
        };

        _client.DisconnectedAsync += args =>
        {
            _subscribed = false;
            _connectionState.MarkDisconnected(DateTimeOffset.UtcNow);
            if (args.Exception is null)
            {
                _logger.LogWarning("Disconnected from MQTT broker.");
            }
            else
            {
                _logger.LogWarning(args.Exception, "Disconnected from MQTT broker due to error.");
            }
            return Task.CompletedTask;
        };

        _client.ApplicationMessageReceivedAsync += async args =>
        {
            var payload = args.ApplicationMessage.PayloadSegment;
            var topic = args.ApplicationMessage.Topic;
            var receivedAt = DateTimeOffset.UtcNow;

            if (_waterLevelHandler.CanHandle(topic))
            {
                await _waterLevelHandler.HandleAsync(topic, payload, args.ApplicationMessage.Retain, receivedAt);
            }
            else if (_pumpStateHandler.CanHandle(topic))
            {
                await _pumpStateHandler.HandleAsync(topic, payload, args.ApplicationMessage.Retain, receivedAt);
            }
            else if (_alarmHandler.CanHandle(topic))
            {
                await _alarmHandler.HandleAsync(topic, payload, args.ApplicationMessage.Retain, receivedAt);
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    await TryConnectAsync(stoppingToken);
                }

                if (_client.IsConnected && !_subscribed)
                {
                    await SubscribeAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT client loop error. Retrying in {DelaySeconds}s.", _options.ReconnectSeconds);
                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectSeconds), stoppingToken);
            }
        }

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(cancellationToken: stoppingToken);
        }
    }

    /// <summary>
    /// Attempts to connect using current MQTT options and retries on failure.
    /// </summary>
    private async Task TryConnectAsync(CancellationToken stoppingToken)
    {
        try
        {
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_options.Host, _options.Port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_options.KeepAliveSeconds))
                .WithClientId(_options.ClientId ?? $"watering-backend-{Guid.NewGuid():N}");

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                builder = builder.WithCredentials(_options.Username, _options.Password);
            }

            if (_options.UseTls)
            {
                builder = builder.WithTlsOptions(_ => { });
            }

            var options = builder.Build();
            await _client.ConnectAsync(options, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Failed to connect to MQTT broker. Retrying in {DelaySeconds}s.", _options.ReconnectSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectSeconds), stoppingToken);
        }
    }

    /// <summary>
    /// Subscribes to the configured topics once per connection.
    /// </summary>
    private async Task SubscribeAsync(CancellationToken stoppingToken)
    {
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(new MqttTopicFilterBuilder()
                .WithTopic(_topics.WaterLevelState)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build())
            .WithTopicFilter(new MqttTopicFilterBuilder()
                .WithTopic(_topics.PumpState)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build())
            .WithTopicFilter(new MqttTopicFilterBuilder()
                .WithTopic(_topics.SystemAlarm)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build())
            .Build();

        await _client.SubscribeAsync(options, stoppingToken);
        _subscribed = true;
        _logger.LogInformation("Subscribed to MQTT topics: {Topics}.", string.Join(", ", new[]
        {
            _topics.WaterLevelState,
            _topics.PumpState,
            _topics.SystemAlarm
        }));
    }
}
