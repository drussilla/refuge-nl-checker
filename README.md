This is a very simple C# program that checks slot availability on https://portaal.refugeepass.nl/make-an-appointment and reports it in Telegram chat so Ukrainian refugees can make an appointment without constantly checking availability manually.

Build and run
==

- Install .NET 6
- Clone repo
- Open `RefugeNlChecker` folder in terminal
- Execute `dotnet run`


Telegram Integration
==

- [Create telegram bot]([url](https://core.telegram.org/bots#6-botfather))
- Copy bot token
- Start conversation with your bot
- Find out your chatId by talking to https://t.me/RawDataBot bot
- Set `secret`, `publicChatId` and `privateChatId` environment varialbes
