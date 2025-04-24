using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient("7930521042:AAFoFPdBteezf7fxQg9DHOxVC9H8jWEG9dM");

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

bool isRunning = true;

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();
Console.WriteLine($"Bot {me.Username} is running");
await Task.Delay(-1);

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = true;
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Bot is now running!",
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("/stop", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = false;
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Bot is now stopped. Send /start to run again.",
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("ping", StringComparison.OrdinalIgnoreCase) && isRunning)
    {
        try
        {
            var pingResult = await PingHostAsync("185.140.192.85");
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Ping result: {pingResult}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Ping error: {ex.Message}",
                cancellationToken: cancellationToken);
        }
    }
    else if (isRunning)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Unknown command. Available commands: /start, /stop, ping",
            cancellationToken: cancellationToken);
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

async Task<string> PingHostAsync(string host)
{
    try
    {
        using var ping = new System.Net.NetworkInformation.Ping();
        var reply = await ping.SendPingAsync(host, 1000);
        
        if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
        {
            return $"Success - Roundtrip: {reply.RoundtripTime}ms";
        }
        return $"Failed - Status: {reply.Status}";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}";
    }
}
