using Akka.Actor;
using Akka.Streams;

namespace MarketRateInfo.Server.Akka;

/// <summary>
/// Owns the Akka.NET ActorSystem and Streams materializer used by the application.
/// Registered as a singleton so the application does not create an ActorSystem
/// for every HTTP request or refresh operation.
/// </summary>
public sealed class AkkaRuntime : IDisposable
{
    private readonly ActorSystem _actorSystem;

    /// <summary>
    /// Initializes a new instance of the AkkaRuntime class and creates the actor system and materializer.
    /// </summary>
    public AkkaRuntime()
    {
        this._actorSystem = ActorSystem.Create("MarketRateInfoSystem");
        Materializer = this._actorSystem.Materializer();
    }

    public IMaterializer Materializer { get; }

    public void Dispose()
    {
        this._actorSystem.Terminate().Wait(TimeSpan.FromSeconds(10));
    }
}
