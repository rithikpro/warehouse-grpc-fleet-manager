using Grpc.Core;
using Grpc.Net.Client;
using WarehouseGrpc;

var channel = GrpcChannel.ForAddress("https://localhost:7001");
var client = new FleetManager.FleetManagerClient(channel);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { cts.Cancel(); e.Cancel = true; };

// Simulate 5 robots
var tasks = new List<Task>();
for (int i = 0; i < 5; i++)
{
    var robotId = $"Robot-{i}";
    tasks.Add(Task.Run(async () => await SimulateRobot(client, robotId, cts.Token)));
}

await Task.WhenAll(tasks);


static async Task SimulateRobot(FleetManager.FleetManagerClient client, string robotId, CancellationToken ct)
{
    using var call = client.ConnectRobot(cancellationToken: ct);

    _ = Task.Run(async () =>
    {
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            var isIdle = random.NextDouble() > 0.5;
            var status = isIdle ? "idle" : "picking";

            var telemetry = new TelemetryRequest
            {
                RobotId = robotId,
                X = random.NextDouble() * 100,
                Y = random.NextDouble() * 100,
                BatteryPct = 100 - (random.NextDouble() * 20),
                Status = status,
                Payload = isIdle ? string.Empty : "SKU123"
            };
            await call.RequestStream.WriteAsync(telemetry);
            await Task.Delay(2000, ct);  
        }
    });

    // Receive commands
    await foreach (var response in call.ResponseStream.ReadAllAsync(ct))
    {
        Console.WriteLine($"{robotId} received: {response.CommandCase}");
        if (response.Task != null)
        {
            Console.WriteLine($"  Task {response.Task.TaskId}: {response.Task.Action} {response.Task.Location}");

            await Task.Delay(5000);
        }
    }
}

