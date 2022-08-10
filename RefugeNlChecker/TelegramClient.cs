using System.Net;
using Microsoft.Extensions.Options;

namespace RefugeNlChecker;

public record TelegramClientOptions(string? secret, string? publicChatId, string? privateChatId)
{
    public TelegramClientOptions() : this(default, default, default) { }
}

public class TelegramClient : ITelegramClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<TelegramClientOptions> _options;

    public TelegramClient(HttpClient httpClient, IOptions<TelegramClientOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public Task SendMessage(string message, CancellationToken cancellationToken)
    {
        var encodedMessage = WebUtility.UrlEncode(message);
        return _httpClient.GetAsync(
            $"https://api.telegram.org/bot{_options.Value.secret}/sendMessage?chat_id={_options.Value.publicChatId}&text={encodedMessage}", 
            cancellationToken);
    }

    public Task SendPrivateMessage(string message, CancellationToken cancellationToken)
    {
        var encodedMessage = WebUtility.UrlEncode(message);
        return _httpClient.GetAsync(
            $"https://api.telegram.org/bot{_options.Value.secret}/sendMessage?chat_id={_options.Value.privateChatId}&text={encodedMessage}",
            cancellationToken);
    }
}

public interface ITelegramClient
{
    Task SendMessage(string message, CancellationToken cancellationToken);
    Task SendPrivateMessage(string message, CancellationToken cancellationToken);
}