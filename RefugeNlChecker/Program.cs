using System.Net;
using System.Net.Http.Json;
using RefugeNlChecker;

Console.WriteLine("Start https://portaal.refugeepass.nl/en/make-an-appointment");

using var handler = new HttpClientHandler { UseCookies = false };
using var client = new HttpClient(handler);

List<string> cookies = new List<string>();
string token = string.Empty;
await GetNewToken();

using var telegramClient = new HttpClient();
var telegramChatId = Environment.GetEnvironmentVariable("TG_ChatId");
var telegramBotSecret = Environment.GetEnvironmentVariable("TG_Secret");

await SendMessage("Start!%20https://portaal.refugeepass.nl/en/make-an-appointment");

try
{
    while (true)
    {
        var date = DateTime.Now.AddDays(2);

        while (date < new DateTime(2022, 10, 01))
        {
            if (await CheckEndpoint(date, "https://post.refugeepass.nl/api/v1/appointment/get-available-options")) return;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            if (await CheckEndpoint(date, "https://post.refugeepass.nl/api/v1/appointment/get-alternative-options")) return;

            date = date.AddDays(1);
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        Thread.Sleep(TimeSpan.FromSeconds(60));
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message} \r\nStartTrace: {ex.StackTrace}");
    await SendMessage(ex.Message);
    throw;
}

async Task GetNewToken()
{
    Console.WriteLine("Getting new token");
    var response = await client.GetAsync("https://portaal.refugeepass.nl/en/make-an-appointment");
    response.EnsureSuccessStatusCode();
    cookies = response.Headers.GetValues("set-cookie").ToList();
    token = ExtractCookie(cookies.First(x => x.StartsWith("XSRF-TOKEN"))).Value.Replace("%3D", "=");
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

Task SendMessage(string message)
{
    return telegramClient.GetAsync($"https://api.telegram.org/bot{telegramBotSecret}/sendMessage?chat_id={telegramChatId}&text={message}");
}

async Task<bool> CheckEndpoint(DateTime selectedDate, string url)
{
    HttpRequestMessage CreateRequest(DateTime dateTime, string endpoint)
    {
        var data = new Root
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

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequestMessage.Headers.Add("Cookie", string.Join("; ", cookies));
        httpRequestMessage.Headers.Add("x-xsrf-token", token);
        httpRequestMessage.Content = JsonContent.Create(data);
        return httpRequestMessage;
    }

    using var request = CreateRequest(selectedDate, url);

    var resp = await client.SendAsync(request);
    if ((int)resp.StatusCode == 419)
    {
        Console.WriteLine("Token expired!");
        await GetNewToken();
        return false;
    }
    
    if (resp.StatusCode != HttpStatusCode.OK)
    {
        Console.WriteLine($"{selectedDate} Unexpected error code: {resp.StatusCode}");
        await SendMessage($"ErrorCode:{resp.StatusCode}");
        return false;
    }

    var rateLeft = resp.Headers.GetValues("x-ratelimit-remaining").FirstOrDefault();

    var restStr = await resp.Content.ReadAsStringAsync();
    if (restStr != "[]")
    {
        Console.WriteLine($"FOUND! {selectedDate}\r\n\r\nREST: {restStr}");
        
        await SendMessage("FOUND!%20https://portaal.refugeepass.nl/en/make-an-appointment");
        await SendMessage($"{restStr}");
        return true;
    }

    Console.WriteLine($"{selectedDate:yyyy-MM-dd} nothing :( rate left: {rateLeft}");
    return false;
}

