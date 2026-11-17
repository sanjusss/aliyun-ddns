using System.Reflection;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Hosting;
using AliyunDdns.Infrastructure.Aliyun;
using AliyunDdns.Infrastructure.Ip;
using AliyunDdns.Infrastructure.Notifications;
using AliyunDdns.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AliyunDdns;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        CommandLineOptions commandLine;
        try
        {
            commandLine = CommandLineOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        if (commandLine.ShowHelp)
        {
            Console.WriteLine(CommandLineOptions.Help);
            return 0;
        }

        if (commandLine.ShowVersion)
        {
            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown");
            return 0;
        }

        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = [],
                ContentRootPath = AppContext.BaseDirectory
            });
            builder.Configuration.Sources.Clear();
            builder.Configuration.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(commandLine.GetConfigPath(AppContext.BaseDirectory, Environment.CurrentDirectory), optional: !commandLine.ConfigSpecified, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddCommandLine(commandLine.ConfigurationArguments.ToArray());

            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
                options.UseUtcTimestamp = true;
            });
            builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);

            var boundOptions = DdnsOptionsBinder.Bind(builder.Configuration);
            builder.Services.AddAliyunDdnsServices(boundOptions);
            if (!commandLine.RunOnce && !commandLine.ValidateOnly)
            {
                builder.Services.AddHostedService<DdnsWorker>();
            }

            using var host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var options = host.Services.GetRequiredService<IOptions<DdnsOptions>>().Value;
            if (commandLine.LegacyValues.Count > 0)
            {
                logger.LogWarning("检测到旧命令行参数 {Variables}；这些参数将在后续版本移除。",
                    string.Join(',', commandLine.LegacyValues.Keys));
            }

            var errors = new LegacyOptionsReader().Apply(options, builder.Configuration, logger,
                name => commandLine.LegacyValues.TryGetValue(name, out var value)
                    ? value
                    : Environment.GetEnvironmentVariable(name)).ToList();
            if (commandLine.DryRunSpecified)
            {
                options.Updater.DryRun = commandLine.DryRun;
            }

            errors.AddRange(OptionsValidator.Validate(options));
            if (errors.Count == 0)
            {
                try
                {
                    options.Aliyun.AccessKeySecret = SecretResolver.ResolveAccessKeySecret(options);
                    if (string.IsNullOrWhiteSpace(options.Aliyun.AccessKeySecret))
                    {
                        errors.Add("AccessKey Secret 文件内容为空。");
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    errors.Add($"读取 AccessKey Secret 失败：{exception.Message}");
                }
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    logger.LogError("配置错误：{Error}", error);
                }

                return 2;
            }

            if (commandLine.ValidateOnly)
            {
                logger.LogInformation("配置校验通过，共 {TargetCount} 个目标。", options.Targets.Count);
                return 0;
            }

            if (commandLine.RunOnce)
            {
                await host.StartAsync();
                try
                {
                    var result = await host.Services.GetRequiredService<DdnsUpdateService>()
                        .RunOnceAsync(host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
                    return result.HasFailures ? 1 : 0;
                }
                finally
                {
                    await host.StopAsync();
                }
            }

            await host.RunAsync();
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"启动失败：{exception.Message}");
            return 2;
        }
    }
}
