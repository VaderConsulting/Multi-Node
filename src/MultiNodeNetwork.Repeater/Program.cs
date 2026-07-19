using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;

using Serilog;

namespace MultiNodeNetwork.Repeater;
public class Program
{
    #region Private Methods

    private static void AddPlatformSpecificConfiguration(IConfigurationBuilder config)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows-specific configuration overrides
            _ = config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NetworkConfig:Performance:MaxConcurrentConnections"] = "20",
                ["NetworkConfig:Performance:MessageQueueSize"] = "500",
                ["NetworkConfig:Performance:BufferSize"] = "4096",
                ["NetworkConfig:Performance:ThreadPoolSize"] = "5",
                ["NetworkConfig:Logging:LogDirectory"] = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MultiNodeNetwork", "logs")
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux/Raspberry Pi-specific configuration overrides
            bool isRaspberryPi = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            if (isRaspberryPi)
            {
                // Raspberry Pi optimized settings
                _ = config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NetworkConfig:Performance:MaxConcurrentConnections"] = "15",
                    ["NetworkConfig:Performance:MessageQueueSize"] = "250",
                    ["NetworkConfig:Performance:BufferSize"] = "2048",
                    ["NetworkConfig:Performance:ThreadPoolSize"] = "3",
                    ["NetworkConfig:Logging:LogDirectory"] = "/var/log/multinodenetwork"
                });
            }
            else
            {
                // Linux x64 settings
                _ = config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NetworkConfig:Performance:MaxConcurrentConnections"] = "18",
                    ["NetworkConfig:Performance:MessageQueueSize"] = "400",
                    ["NetworkConfig:Performance:BufferSize"] = "3072",
                    ["NetworkConfig:Performance:ThreadPoolSize"] = "4",
                    ["NetworkConfig:Logging:LogDirectory"] = "/var/log/multinodenetwork"
                });
            }
        }
    }

    private static string GetPlatformLogDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiNodeNetwork", "logs");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "/var/log/multinodenetwork";
        }

        return "logs";
    }

    #endregion

    #region Public Methods

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                _ = config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("../../appsettings.json", optional: false, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);

                // Add platform-specific configuration
                AddPlatformSpecificConfiguration(config);
            })
            .ConfigureServices((context, services) =>
            {
                // Platform service
                _ = services.AddSingleton<PlatformService>();

                // Configuration
                NetworkConfig networkConfig = new NetworkConfig();
                context.Configuration.GetSection("NetworkConfig").Bind(networkConfig);
                _ = services.AddSingleton(networkConfig);

                // Core services
                _ = services.AddSingleton<WifiManager>();
                _ = services.AddSingleton<MessageHandler>();
                _ = services.AddSingleton<NodeIdManager>();
                _ = services.AddSingleton<NetworkInterfaceService>();
                _ = services.AddSingleton<IpAddressMonitorService>();

                // Protocols
                _ = services.AddSingleton<MasterElection>();
                _ = services.AddSingleton<NodeDiscovery>();
                _ = services.AddSingleton<Communication>();
                _ = services.AddSingleton<Topology>();

                // Background services
                _ = services.AddHostedService<RepeaterNodeService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                _ = logging.ClearProviders();

                // Get platform-specific log directory
                string logDirectory = GetPlatformLogDirectory();
                string logFilePath = Path.Combine(logDirectory, "repeater-node-.log");

                // Configure Serilog
                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .CreateLogger();

                _ = logging.AddSerilog();
            })
            .UseSerilog();
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== MultiNode Network Repeater Node ===");
        Console.WriteLine("Starting repeater node application...");

        try
        {
            IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    #endregion
}
