using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BlazorDiffusion;

public class TrackingCircuitHandler : CircuitHandler
{
    private HashSet<Circuit> circuits = new();

    ILogger<TrackingCircuitHandler> log;
    public TrackingCircuitHandler(ILogger<TrackingCircuitHandler> log)
    {
        this.log = log;
    }

    public override Task OnConnectionUpAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        log.LogDebug("Circuit Connection {0} Opened", circuit.Id);
        circuits.Add(circuit);

        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        log.LogDebug("Circuit Connection {0} Closed", circuit.Id);
        circuits.Remove(circuit);

        return Task.CompletedTask;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        log.LogDebug("Circuit {0} Opened", circuit.Id);
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        log.LogDebug("Circuit {0} Closed", circuit.Id);
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }

    public int ConnectedCircuits => circuits.Count;
}
