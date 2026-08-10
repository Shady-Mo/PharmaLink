namespace Application.DTOs.Routing;

/// <summary>
/// Result of an OSRM <c>/table</c> (distance-matrix) request. A single HTTP call returns the
/// driving distance between every pair of the supplied coordinates, so the routing engine can
/// evaluate many branch combinations without issuing one request per leg.
/// </summary>
public sealed class OsrmMatrixResultDto
{
    /// <summary>
    /// Square matrix of driving distances in kilometres. <c>DistancesKm[i][j]</c> is the distance
    /// from coordinate <c>i</c> to coordinate <c>j</c> (0 on the diagonal). A value of
    /// <see cref="double.MaxValue"/> marks an unreachable / unroutable pair.
    /// The coordinate order matches the order the coordinates were supplied in.
    /// </summary>
    public double[][] DistancesKm { get; init; } = [];

    /// <summary>
    /// Square matrix of driving durations in MINUTES. <c>DurationsMinutes[i][j]</c> is the travel
    /// time from coordinate <c>i</c> to coordinate <c>j</c> (0 on the diagonal). A value of
    /// <see cref="double.MaxValue"/> marks an unreachable / unroutable pair. Used to compute a
    /// delivery ETA so a branch that would close before the driver arrives can be excluded.
    /// </summary>
    public double[][] DurationsMinutes { get; init; } = [];

    /// <summary>Number of coordinates (matrix dimension).</summary>
    public int Size => DistancesKm.Length;


    /// <summary>True when OSRM returned a usable matrix; false on any failure.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Human-readable status / error message.</summary>
    public string Message { get; init; } = string.Empty;
}
