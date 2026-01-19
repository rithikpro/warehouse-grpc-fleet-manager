using Grpc.Core;
using WarehouseGrpc;
using System.Collections.Concurrent;

namespace GrpcService1.Services
{
    public class FleetManagerService : FleetManager.FleetManagerBase
    {

        private readonly ILogger<FleetManagerService> _logger;
        private readonly List<TaskAssignment> _pendingTasks = new()
        {
            new() { TaskId = "T1", Action = "pick",   Location = "Aisle-5",  ItemId = "SKU123" },
            new() { TaskId = "T2", Action = "move",   Location = "Aisle-3",  ItemId = "SKU456" },
            new() { TaskId = "T3", Action = "restock",Location = "Aisle-8",  ItemId = "SKU789" },
            new() { TaskId = "T4", Action = "inspect",Location = "Aisle-1",  ItemId = "SKU321" }
        };


        public FleetManagerService(ILogger<FleetManagerService> logger) => _logger = logger;

        public override async Task ConnectRobot(IAsyncStreamReader<TelemetryRequest> requestStream,
            IServerStreamWriter<FleetCommand> responseStream, ServerCallContext context)
        {
            var robotId = "";

            try
            {
                await foreach (var req in requestStream.ReadAllAsync())
                {
                    robotId = req.RobotId;
                    _logger.LogInformation("Telemetry from {RobotId}: ({X},{Y}) {Battery}% {Status}",
                        robotId, req.X, req.Y, req.BatteryPct, req.Status);

                    // Assign task if idle and pending tasks exist
                    if (req.Status == "idle" && _pendingTasks.Any())
                    {
                        var task = _pendingTasks.First();
                        _pendingTasks.Remove(task);
                        await responseStream.WriteAsync(new FleetCommand
                        {
                            RobotId = robotId,
                            Task = task
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Robot {RobotId} stream error", robotId);
            }
        }
    }

}
