using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// توكن البوت الخاص بك
var botClient = new TelegramBotClient("7930521042:AAFoFPdBteezf7fxQg9DHOxVC9H8jWEG9dM");

using CancellationTokenSource cts = new();

// إعدادات استقبال الرسائل
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

bool isRunning = true;

// بدء استقبال الرسائل
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();
Console.WriteLine($"البوت {me.Username} يعمل الآن");
await Task.Delay(-1); // إبقاء التطبيق يعمل

// دالة معالجة الرسائل
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"تم استقبال رسالة '{messageText}' في الدردشة {chatId}.");

    if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = true;
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "البوت يعمل الآن!",
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("/stop", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = false;
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "تم إيقاف البوت. أرسل /start لتشغيله مرة أخرى.",
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("ping", StringComparison.OrdinalIgnoreCase) && isRunning)
    {
        try
        {
            var pingResult = PingHost("185.140.192.85");
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"نتيجة ping: {pingResult}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"حدث خطأ أثناء تنفيذ ping: {ex.Message}",
                cancellationToken: cancellationToken);
        }
    }
    else if (isRunning)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "أمر غير معروف. الأوامر المتاحة: /start, /stop, ping",
            cancellationToken: cancellationToken);
    }
}

// دالة معالجة الأخطاء
Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"خطأ في API تلجرام:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

// دالة تنفيذ ping
string PingHost(string host)
{
    using var ping = new System.Net.NetworkInformation.Ping();
    try
    {
        var reply = ping.Send(host, 1000); // مهلة ثانية واحدة
        if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
        {
            return $"نجاح - وقت الاستجابة: {reply.RoundtripTime}ms";
        }
        return $"فشل - الحالة: {reply.Status}";
    }
    catch (Exception ex)
    {
        return $"خطأ: {ex.Message}";
    }
}
