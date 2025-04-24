using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.NetworkInformation;

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
            var pingResult = await PingService.PingHost("8.8.8.8");
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: pingResult,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Ping failed: {ex.Message}",
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

public static class PingService
{
    public static async Task<string> PingHost(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 3000);
            
            if (reply.Status == IPStatus.Success)
            {
                return $"✅ Ping successful!\n" +
                       $"🔹 IP: {host}\n" +
                       $"🔹 Time: {reply.RoundtripTime}ms\n" +
                       $"🔹 TTL: {reply.Options?.Ttl ?? 0}";
            }
            return $"❌ Ping failed\n" +
                   $"🔹 Status: {reply.Status}\n" +
                   $"🔹 IP: {host}";
        }
        catch (PingException ex)
        {
            return $"❌ Ping error\n" +
                   $"🔹 IP: {host}\n" +
                   $"🔹 Error: {ex.InnerException?.Message ?? ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ General error\n" +
                   $"🔹 IP: {host}\n" +
                   $"🔹 Error: {ex.Message}";
        }
    }
}
