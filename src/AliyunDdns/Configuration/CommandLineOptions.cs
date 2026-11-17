namespace AliyunDdns.Configuration;

public sealed class CommandLineOptions
{
    public bool RunOnce { get; private set; }

    public bool DryRun { get; private set; }

    public bool DryRunSpecified { get; private set; }

    public bool ValidateOnly { get; private set; }

    public bool ShowHelp { get; private set; }

    public bool ShowVersion { get; private set; }

    public string ConfigPath { get; private set; } = "appsettings.json";

    public bool ConfigSpecified { get; private set; }

    public IList<string> ConfigurationArguments { get; } = new List<string>();

    public IDictionary<string, string> LegacyValues { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static CommandLineOptions Parse(string[] args)
    {
        var result = new CommandLineOptions();
        for (var index = 0; index < args.Length; index++)
        {
            var (name, inlineValue, hasInlineValue) = SplitOption(args[index]);
            switch (name)
            {
                case "--once": result.RunOnce = ReadBoolean(name, inlineValue, hasInlineValue); break;
                case "--dry-run":
                    result.DryRun = ReadBoolean(name, inlineValue, hasInlineValue);
                    result.DryRunSpecified = true;
                    break;
                case "--validate-config": result.ValidateOnly = ReadBoolean(name, inlineValue, hasInlineValue); break;
                case "--help": result.ShowHelp = ReadBoolean(name, inlineValue, hasInlineValue); break;
                case "--version": result.ShowVersion = ReadBoolean(name, inlineValue, hasInlineValue); break;
                case "--config":
                    result.ConfigPath = ReadValue(args, ref index, name, inlineValue, hasInlineValue);
                    result.ConfigSpecified = true;
                    break;
                case "-u": case "--access-key-id": result.LegacyValues["AKID"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "-p": case "--access-key-secret": result.LegacyValues["AKSCT"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "-d": case "--domain": result.LegacyValues["DOMAIN"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "--root-domain": result.LegacyValues["ROOT_DOMAIN"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "-i": case "--interval": result.LegacyValues["REDO"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "-t": case "--ttl": result.LegacyValues["TTL"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "--timezone": result.LegacyValues["TIMEZONE"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "--type": result.LegacyValues["TYPE"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "--cnipv4": result.LegacyValues["CNIPV4"] = ReadBoolean(name, inlineValue, hasInlineValue) ? "true" : "false"; break;
                case "-h": case "--webhook": result.LegacyValues["WEBHOOK"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "-c": case "--checklocal": result.LegacyValues["CHECKLOCAL"] = ReadBoolean(name, inlineValue, hasInlineValue) ? "true" : "false"; break;
                case "--ipv4nets": result.LegacyValues["IPV4NETS"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                case "--ipv46nets": case "--ipv6nets": result.LegacyValues["IPV6NETS"] = ReadValue(args, ref index, name, inlineValue, hasInlineValue); break;
                default:
                    if (name.StartsWith("--", StringComparison.Ordinal) && name.Contains(':', StringComparison.Ordinal))
                    {
                        result.ConfigurationArguments.Add(args[index]);
                        if (!hasInlineValue)
                        {
                            if (++index >= args.Length || IsKnownOption(args[index]))
                            {
                                throw new ArgumentException($"{name} 需要参数值。");
                            }

                            result.ConfigurationArguments.Add(args[index]);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"未知参数：{args[index]}");
                    }

                    break;
            }
        }

        return result;
    }

    public string GetConfigPath(string applicationBaseDirectory, string currentWorkingDirectory)
    {
        return ConfigSpecified
            ? Path.GetFullPath(ConfigPath, currentWorkingDirectory)
            : Path.GetFullPath(ConfigPath, applicationBaseDirectory);
    }

    private static (string Name, string? Value, bool HasValue) SplitOption(string argument)
    {
        var separator = argument.IndexOf('=');
        return separator < 0
            ? (argument, null, false)
            : (argument[..separator], argument[(separator + 1)..], true);
    }

    private static bool ReadBoolean(string name, string? value, bool hasValue)
    {
        if (!hasValue)
        {
            return true;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        throw new ArgumentException($"{name} 的布尔值只能是 true 或 false。");
    }

    private static string ReadValue(
        string[] args,
        ref int index,
        string name,
        string? inlineValue,
        bool hasInlineValue)
    {
        if (hasInlineValue)
        {
            if (string.IsNullOrWhiteSpace(inlineValue))
            {
                throw new ArgumentException($"{name} 需要参数值。");
            }

            return inlineValue;
        }

        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || IsKnownOption(args[index]))
        {
            throw new ArgumentException($"{name} 需要参数值。");
        }

        return args[index];
    }

    private static bool IsKnownOption(string argument)
    {
        var name = SplitOption(argument).Name;
        return name is "--once" or "--dry-run" or "--validate-config" or "--help" or "--version" or "--config" or
            "-u" or "--access-key-id" or "-p" or "--access-key-secret" or "-d" or "--domain" or
            "--root-domain" or "-i" or "--interval" or "-t" or "--ttl" or "--timezone" or "--type" or
            "--cnipv4" or "-h" or "--webhook" or "-c" or "--checklocal" or "--ipv4nets" or "--ipv46nets" or "--ipv6nets";
    }

    public static string Help => """
        阿里云 DDNS

        用法：aliyun-ddns [选项] [--配置节:参数=值]

          --once              只执行一轮更新。
          --dry-run           输出更新计划，不执行 DNS 写操作。
          --validate-config   只校验配置，成功返回 0，失败返回 2。
          --config <路径>     指定 JSON 配置文件；显式相对路径按当前工作目录解析。
          --help              显示帮助。
          --version           显示版本。

        已知长参数支持 --name=value；布尔开关支持 =true 和 =false。
        未知开关和位置参数会返回配置错误。新版 --节:键=值 形式保持可用。
        Local 默认排除 docker*/br-*/veth*/vbr* 等容器网桥；AllowAnyInterface=true 可绕过。

        0.3.x 暂时保留旧参数 -u、-p、-d、-i、-t、--root-domain、--timezone、
        --type、--cnipv4、--webhook、--checklocal、--ipv4nets、--ipv46nets。
        旧参数均会输出弃用提示；为兼容旧版本，-h 仍表示 --webhook。

        示例：
          aliyun-ddns --once --dry-run=true
          aliyun-ddns --Updater:IntervalSeconds=300
        """;
}
