using System.Net;
using System.Net.Http.Json;
using RefugeNlChecker;

Console.WriteLine("Start https://portaal.refugeepass.nl/en/make-an-appointment");

using var handler = new HttpClientHandler {UseCookies = false};

using var client = new HttpClient(handler) { BaseAddress = new Uri("https://post.refugeepass.nl") };

using var telegramClient = new HttpClient();
var telegramBotSecret = Environment.GetEnvironmentVariable("TG_Secret");

await SendMessage("Start!%20https://portaal.refugeepass.nl/en/make-an-appointment");

try
{
    while (true)
    {
        var date = DateTime.Now.AddDays(2);

        while (date < new DateTime(2022, 10, 01))
        {
            if (await CheckEndpoint(date, "/api/v1/appointment/get-available-options")) return;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            if (await CheckEndpoint(date, "/api/v1/appointment/get-alternative-options")) return;

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

Task SendMessage(string message)
{
    return telegramClient.GetAsync($"https://api.telegram.org/bot{telegramBotSecret}/sendMessage?chat_id=423630263&text={message}");
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

        var token =
            "eyJpdiI6InFVVTZrM2hXNEU5ejhGeFdMNEozL0E9PSIsInZhbHVlIjoiZmdJa25ZQ2g2RjhEbEdKYW5XZVI1N0hROGlwdGVTdUNsMFhqVit1SERNK1FOTDVFWCthbUl5TEJxRGFxSjlCYXQ2OHhLc1NqR3NabmFuSmpTNXdvSDI1a2lUT203bDdyQzdUZ3E3YThUZmpLQzlyUWhHSW03ZzNHeVdrMWZ0aTgiLCJtYWMiOiI1MmEyNjEwNGUxODAyMzExMDY0YTQxYmZmMWIwZTkzMGMyN2RhYWI0Yjk0N2U4NTVjNGFjMGM3ZTVkNjIyZjhlIiwidGFnIjoiIn0=";

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequestMessage.Headers.Add("Cookie",
            "XSRF-TOKEN=eyJpdiI6InFVVTZrM2hXNEU5ejhGeFdMNEozL0E9PSIsInZhbHVlIjoiZmdJa25ZQ2g2RjhEbEdKYW5XZVI1N0hROGlwdGVTdUNsMFhqVit1SERNK1FOTDVFWCthbUl5TEJxRGFxSjlCYXQ2OHhLc1NqR3NabmFuSmpTNXdvSDI1a2lUT203bDdyQzdUZ3E3YThUZmpLQzlyUWhHSW03ZzNHeVdrMWZ0aTgiLCJtYWMiOiI1MmEyNjEwNGUxODAyMzExMDY0YTQxYmZmMWIwZTkzMGMyN2RhYWI0Yjk0N2U4NTVjNGFjMGM3ZTVkNjIyZjhlIiwidGFnIjoiIn0%3D; refugee_pass_session=eyJpdiI6IkhYdXBSeU0wZXZPbEtIV011dm45dFE9PSIsInZhbHVlIjoiTDZKYWtrbnRYbmZuaVN3VStsNFl4ZDY4SXNmbU8xNFBJczdBdlZHeVJPNm5QWDR5K0tuVWpha25KL2pmZ0lQcVczWW5zS0NTU2kvek9wdlNqTmZ6MlIzazd4TjRVZExBMWdTV2xsa3lCclJCSEJsOFp5NitLL25WUkJRLy9oN00iLCJtYWMiOiJmNWEwZWJmNDgyMDk1YjViZTY1OTI1MzZmNmU1OWJhYjZjNThiYTM2ZmZiNjllMTZlYjY5MjZiNjkxNjgwN2JlIiwidGFnIjoiIn0%3D; locale=eyJpdiI6IkVYUnZ0aUJsd3R4WTQrOXN4TTcwWVE9PSIsInZhbHVlIjoiT0l2Vjl4L1NKcEdIYzZEdjBWQXFxQkpOL0RFN1k0U2NpaTlvUmtnQ2dlVG9xVDBnSVpFTng5WS9uYkQ2aXdULyIsIm1hYyI6IjEwYzgzZTc3ZmQwOTliMWI4ODYzZTk4MTVkMDhmZGEwOThjNjMwOTg1NDVhODI1OThlOTIwMDIxMjJhMWMzODciLCJ0YWciOiIifQ%3D%3D; A0ceNtNtHhd6nhGefTG6FVzwh1xrxZm9TFKo0CWR=eyJpdiI6Ii9rd2V1YTNYL2oyUHFubkNVSFFxNlE9PSIsInZhbHVlIjoieUlsblZiVDZYam5Md2NhUEhFV2Qxczhudll4VVpUaC9EaE9PV2xVVnhkSTZGTHNCZzh0Y2NhcFowVjc4YUNjcDRPRjBTY3AwaDJnSjFZRFY0azQ4QmN3MEMraVpWNzlCY1lqaWtvQ3d6VlMwTVpxZVo2VFRKdU1Oa0U1RHRZVEh1TXpLOVhFOG9kSmhUQkZmeGIwbE9ZdE9DNjJhSWR5ZmpGTjBzNGZGOW54YUtsTE96T24vS00wUUN2U01xQXlQd2l5LzlwQ1VUTTBMSFBPV2VGVUJSQkxrcmkxY09qcmlQRXJ4MFBtTkdxUE1pek1KT1FscTdjQ3dOZmNDaWltQXk4aTNmQ042S3pDbVpvWHRBblJiZVM4VlFGTXRyQVoyY1JKQVdnT0tNT0Rzem1KRy9Vd3ZRZ0M2ckVSVXdRRUxqeFF5YWY4QWp0T1NITXdiS3ZPbW41aERVblZKWXdwVWVpL3FjUTJ4TU5wVlErMzR3N1BiUGo2MkJuYmJ1K2c5eEl4TVlNdVZNODAxRHYzZTJYQmxCVS84S3FMVUYzVTgvdko2NGo2cE5vZEIvYTV3QzhCeUVSL0F6L3VLVzFrMCIsIm1hYyI6ImY1ZDM3MzZiNWJkYzk1ZDNmMzIyM2E5OTQ4NGMzNDkwMWNjNjA5NDczYTFiOTNhMGZmMzczOTc2ZTQ3ZDU2NTkiLCJ0YWciOiIifQ%3D%3D");
        httpRequestMessage.Headers.Add("x-xsrf-token", token);
        httpRequestMessage.Content = JsonContent.Create(data);
        return httpRequestMessage;
    }

    using var request = CreateRequest(selectedDate, url);

    var resp = await client.SendAsync(request);
    if ((int)resp.StatusCode == 419)
    {
        throw new Exception("Token expired!");
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

    Console.WriteLine($"{selectedDate} nothing :( rate left: {rateLeft}");
    return false;
}

