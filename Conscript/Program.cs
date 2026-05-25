using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conscript;

static class Program
{
    static void Main(string[] args)
    {
        SteamBootstrap.TryInit();
        try
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddSingleton<IGame, Game>();

            using var host = builder.Build();

            var game = host.Services.GetRequiredService<IGame>();
            game.Run();
        }
        finally
        {
            SteamBootstrap.Shutdown();
        }
    }
}
