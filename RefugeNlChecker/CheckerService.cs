using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RefugeNlChecker;

public class CheckerService : BackgroundService
{
    private readonly ITelegramClient _telegram;
    private readonly HttpClient _client;
    private readonly ILogger<CheckerService> _log;
    private string? _token;
    private List<string>? _cookies;

    private Dictionary<Response, DateTime> _reportedDateTimes = new();

    public CheckerService(ITelegramClient telegram, HttpClient client, ILogger<CheckerService> log)
    {
        _telegram = telegram;
        _client = client;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _telegram.SendPrivateMessage("Executing", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await GetNewToken(stoppingToken);

            var date = DateTime.Now.AddDays(1);

            while (date < new DateTime(2022, 10, 01))
            {
                await CheckEndpoint(date, "https://post.refugeepass.nl/api/v1/appointment/get-alternative-options", stoppingToken);
                date = date.AddDays(1);
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }

    private HttpRequestMessage CreateRequest(DateTime dateTime, string endpoint)
    {
        var data = new Request
        {
            day = new()
            {
                amount = "1",
                date = dateTime.ToString("yyyy-MM-dd")
            },
            place = new() { postcode = "2181" },
            appointment_options = new() { appointment = string.Empty },
            confirm = new(),
            info = new() { telephone_number = string.Empty }
        };

        if (_cookies == null)
        {
            throw new Exception("Cookies are not set. Please call GenerateToken first");
        }

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequestMessage.Headers.Add("Cookie", string.Join("; ", _cookies));
        httpRequestMessage.Headers.Add("x-xsrf-token", _token);
        httpRequestMessage.Content = JsonContent.Create(data);
        return httpRequestMessage;
    }

    private async Task CheckEndpoint(DateTime selectedDate, string url, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(selectedDate, url);

        var resp = await _client.SendAsync(request, cancellationToken);
        if ((int)resp.StatusCode == 419)
        {
            _log.LogWarning("Token expired!");
            await GetNewToken(cancellationToken);
            return;
        }
    
        if (resp.StatusCode != HttpStatusCode.OK)
        {
            _log.LogError($"{selectedDate} Unexpected error code: {resp.StatusCode}");
            await _telegram.SendPrivateMessage($"ErrorCode: {resp.StatusCode}", cancellationToken);
            return;
        }

        var rateLeft = resp.Headers.GetValues("x-ratelimit-remaining").FirstOrDefault();

        var response = await resp.Content.ReadFromJsonAsync<List<Response>>(cancellationToken: cancellationToken);
        if (response is { Count: > 0 })
        {
            foreach (var data in response)
            {
                _log.LogInformation($"FOUND! {selectedDate}\r\n\r\nDate: {data.date}\r\nTime: {data.time}\r\nLocation: {data.location_data.name}\r\nAddress: {data.location_data.address}");
                
                if (_reportedDateTimes.TryGetValue(data, out var dateReported) && DateTime.UtcNow - dateReported < TimeSpan.FromMinutes(5))
                {
                    _log.LogInformation("This was reported less than 5 minutes ago, skip this time slot");
                    continue;
                }

                _log.LogInformation("Reporting time slot to the chat");
                _reportedDateTimes[data] = DateTime.UtcNow;
                var message = "Знайдено вільне місце для 1 людини.\r\n" +
                              $"Дата: {data.date}\r\n" +
                              $"Час: {data.time}\r\n" +
                              $"Місто: {data.location_data.name}\r\n" +
                              $"Адреса: {data.location_data.address}\r\n" +
                              "\r\n" +
                              "Реестрація: https://portaal.refugeepass.nl/uk/make-an-appointment";

                await _telegram.SendMessage(message, cancellationToken);
            }
        }
        else
        {
            _log.LogInformation($"{selectedDate:yyyy-MM-dd} nothing :( rate left: {rateLeft}");
        }

        CleanUpExpiredCache();
    }

    private void CleanUpExpiredCache()
    {
        foreach (var reportedDateTime in _reportedDateTimes)
        {
            if (DateTime.UtcNow - reportedDateTime.Value > TimeSpan.FromSeconds(10))
            {
                _reportedDateTimes.Remove(reportedDateTime.Key);
            }
        }
    }

    private async Task GetNewToken(CancellationToken cancellationToken)
    {
        _log.LogInformation("Getting new token");
        var response = await _client.GetAsync("https://portaal.refugeepass.nl/en/make-an-appointment", cancellationToken);
        response.EnsureSuccessStatusCode();
        _cookies = response.Headers.GetValues("set-cookie").ToList();
        _token = ExtractCookie(_cookies.First(x => x.StartsWith("XSRF-TOKEN"))).Value.Replace("%3D", "=");
    }

    (string Name, string Value) ExtractCookie(string setCookieString)
    {
        var nameValueSplit = setCookieString.IndexOf("=", StringComparison.InvariantCultureIgnoreCase);
        var cookieEnd = setCookieString.IndexOf(";", StringComparison.InvariantCultureIgnoreCase);

        if (nameValueSplit == -1 || cookieEnd == -1)
        {
            throw new FormatException($"Wrong cookie format. Expected <name>=<value>; but got: {setCookieString}");
        }

        var name = setCookieString.Substring(0, nameValueSplit);
        var value = setCookieString.Substring(nameValueSplit + 1, cookieEnd - nameValueSplit - 1);

        return (name, value);
    }
}