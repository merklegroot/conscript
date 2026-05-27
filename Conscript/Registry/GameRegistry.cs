using Microsoft.Extensions.DependencyInjection;

namespace Conscript.Registry;

public static class GameRegistry
{
    public static IServiceCollection RegisterGame(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IGame, Game>();
}
