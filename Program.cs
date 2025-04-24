using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.Http;
using System.Diagnostics;

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
            text: "✅ البوت يعمل الآن!\n" +
                  "الأوامر المتاحة:\n" +
                  "/start - بدء التشغيل\n" +
                  "/stop - إيقاف البوت\n" +
                  "ping - اختبار الاتصال بين google والخادم",
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("/stop", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = false;
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "⛔ البوت متوقف الآن. أرسل /start لإعادة التشغيل",
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("ping", StringComparison.OrdinalIgnoreCase) && isRunning)
    {
        try
        {
            var googleResult = await NetworkService.TestConnection("https://www.google.com");
            var serverResult = await NetworkService.TestConnection("http://huc.edu.iq:9596/");
            
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"🌐 نتائج اختبار الاتصال:\n\n" +
                      $"🔹 اتصال بموقع Google:\n{googleResult}\n\n" +
                      $"🔹 اتصال بالخادم:\n{serverResult}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ فشل اختبار الاتصال\n" +
                      $"الخطأ: {ex.Message}",
                cancellationToken: cancellationToken);
        }
    }
    else if (isRunning)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "⚠️ أمر غير معروف\n" +
                  "الأوامر المتاحة:\n" +
                  "/start - بدء التشغيل\n" +
                  "/stop - إيقاف البوت\n" +
                  "ping - اختبار الاتصال بين google والخادم",
            cancellationToken: cancellationToken);
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"خطأ في تلجرام API:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

public static class NetworkService
{
    public static async Task<string> TestConnection(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync(url);
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                return $"✅ الحالة: ناجح\n" +
                       $"⏱️ وقت الاستجابة: {stopwatch.ElapsedMilliseconds}ms\n" +
                       $"🔢 كود الحالة: {(int)response.StatusCode}";
            }
            
            return $"⚠️ الحالة: مشكلة\n" +
                   $"⏱️ وقت الاستجابة: {stopwatch.ElapsedMilliseconds}ms\n" +
                   $"🔢 كود الحالة: {(int)response.StatusCode}";
        }
        catch (TaskCanceledException)
        {
            return "❌ الحالة: انتهى الوقت المحدد (5 ثواني)";
        }
        catch (HttpRequestException ex)
        {
            return $"❌ الحالة: فشل الاتصال\n" +
                   $"📌 التفاصيل: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ الحالة: خطأ غير متوقع\n" +
                   $"📌 التفاصيل: {ex.Message}";
        }
    }
}
