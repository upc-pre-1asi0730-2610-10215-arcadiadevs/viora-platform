using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Static factory for <see cref="WireMockServer"/> instances that
///     fake the project's outbound HTTP services in integration tests.
///     Each factory method starts a WireMock server on a random port
///     and registers a default stub so the host's outbound call
///     succeeds without per-test setup.
/// </summary>
/// <remarks>
///     <para>
///         The default stubs return EMPTY responses (no data), which
///         is the safe default for tests that do not care about the
///         outbound payload. Tests that DO care register their own
///         <c>Given(...).RespondWith(...)</c> mappings after the
///         factory returns the server; the later mapping wins for
///         matching requests.
///     </para>
///     <para>
///         Mirrors the design §1.6 <c>WireMockBuilders</c> factory
///         contract. The downstream test (F3a.2 etc.) wires the
///         <c>WireMockServer.Url</c> into the production outbound
///         service registration via the harness's
///         <c>ConfigureTestServices</c> hook.
///     </para>
/// </remarks>
public static class WireMockBuilders
{
    /// <summary>
    ///     Starts a <see cref="WireMockServer"/> that fakes the
    ///     <c>IWeatherDataService</c> endpoint. Default stub: an
    ///     empty 7-day forecast JSON for any GET
    ///     <c>/v1/weather/point</c> request.
    /// </summary>
    public static WireMockServer ForWeatherDataService()
    {
        var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/v1/weather/point").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithBody("{\"daily\":[]}")
                  .WithHeader("Content-Type", "application/json"));
        return server;
    }

    /// <summary>
    ///     Starts a <see cref="WireMockServer"/> that fakes the
    ///     <c>IAgroMonitoringImageryService</c> endpoint. Default
    ///     stub: an empty NDVI tile search JSON for any GET
    ///     <c>/v1/image/search</c> request.
    /// </summary>
    public static WireMockServer ForAgroMonitoringImageryService()
    {
        var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/v1/image/search").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithBody("{\"results\":[]}")
                  .WithHeader("Content-Type", "application/json"));
        return server;
    }
}
