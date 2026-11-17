using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using Microsoft.Extensions.Options;

namespace AliyunDdns.Infrastructure.Ip;

public sealed class PublicIpAddressProvider : IIpAddressProvider
{
    private const int MaximumResponseBytes = 1024;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IpSourceCatalog _catalog;
    private readonly DdnsOptions _options;
    private readonly ILogger<PublicIpAddressProvider> _logger;

    public PublicIpAddressProvider(
        IHttpClientFactory httpClientFactory,
        IpSourceCatalog catalog,
        IOptions<DdnsOptions> options,
        ILogger<PublicIpAddressProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _catalog = catalog;
        _options = options.Value;
        _logger = logger;
    }

    public IpAddressSource Source => IpAddressSource.Public;

    public async Task<IPAddress?> GetAddressAsync(
        DdnsTarget target,
        DnsRecordType type,
        CancellationToken cancellationToken)
    {
        foreach (var source in _catalog.Sources.Where(source => source.Type == type &&
                     (type != DnsRecordType.A || !_options.Updater.ChinaIPv4Only || source.Region == IpSourceRegion.China)))
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(_options.Updater.RequestTimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Get, source.Uri);
                using var response = await _httpClientFactory.CreateClient("public-ip")
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();
                var content = await ReadLimitedContentAsync(response, timeout.Token);
                var text = ParseContent(source, content);
                if (IpAddressValidator.TryParsePublic(text, type, out var address))
                {
                    _logger.LogInformation("公网 IP 来源 {SourceName} 返回有效 {RecordType} 地址。", source.Name, type);
                    return address;
                }

                _logger.LogWarning("公网 IP 来源 {SourceName} 返回了无效或非公网地址。", source.Name);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("公网 IP 来源 {SourceName} 请求超时。", source.Name);
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or JsonException)
            {
                _logger.LogWarning("公网 IP 来源 {SourceName} 请求失败：{Error}", source.Name, exception.Message);
            }
        }

        return null;
    }

    private static string ParseContent(HttpIpSource source, string content)
    {
        if (source.Format == IpSourceFormat.PlainText)
        {
            return content.Trim();
        }

        if (string.IsNullOrWhiteSpace(source.JsonPropertyName))
        {
            throw new InvalidDataException("JSON IP 来源缺少属性名。");
        }

        using var document = JsonDocument.Parse(content);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty(source.JsonPropertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException("JSON 响应缺少预期的 IP 属性。");
        }

        return property.GetString() ?? string.Empty;
    }

    private static async Task<string> ReadLimitedContentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[512];
        while (output.Length <= MaximumResponseBytes)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            output.Write(buffer, 0, read);
        }

        if (output.Length > MaximumResponseBytes)
        {
            throw new InvalidDataException("IP 接口响应超过大小限制。");
        }

        return Encoding.UTF8.GetString(output.ToArray());
    }
}
