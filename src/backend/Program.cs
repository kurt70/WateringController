using WateringController.Backend.Mqtt;
using WateringController.Backend.State;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WaterLevelStateStore>();
builder.Services.AddSingleton<WaterLevelMqttHandler>();
var app = builder.Build();

app.MapGet("/", () => "WateringController.Backend");

app.Run();
