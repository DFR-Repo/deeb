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
    var targetUrl = "http://huc.edu.iq:9596/";

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
                  $"/ping - اختبار الاتصال بالخادم",
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
    else if (messageText.Equals("/ping", StringComparison.OrdinalIgnoreCase) && isRunning)
    {
        // إرسال رسالة بدء الاختبار
        var startMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"🔄 جاري اختبار الاتصال بالخادم:\n{targetUrl}\n(5 محاولات)...",
            cancellationToken: cancellationToken);

        for (int i = 1; i <= 5; i++)
        {
            try
            {
                var result = await NetworkService.TestConnection(targetUrl);
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"📊 <b>نتيجة المحاولة #{i}</b>\n" +
                          $"🔗 <b>الرابط:</b> {targetUrl}\n" +
                          $"{result}\n" +
                          $"─────────────────────",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ <b>فشل المحاولة #{i}</b>\n" +
                          $"🔗 <b>الرابط:</b> {targetUrl}\n" +
                          $"الخطأ: {ex.Message}\n" +
                          $"─────────────────────",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }

            if (i < 5) // لا تنتظر بعد المحاولة الأخيرة
            {
                await Task.Delay(1000); // انتظار ثانية بين المحاولات
            }
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
                  "/ping - اختبار الاتصال بالخادم",
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
            client.Timeout = TimeSpan.FromSeconds(3);
            
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync(url);
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                return $"✅ <b>الحالة:</b> ناجح\n" +
                       $"⏱️ <b>الوقت:</b> {stopwatch.ElapsedMilliseconds}ms\n" +
                       $"🔢 <b>كود الحالة:</b> {(int)response.StatusCode}";
            }
            
            return $"⚠️ <b>الحالة:</b> مشكلة\n" +
                   $"⏱️ <b>الوقت:</b> {stopwatch.ElapsedMilliseconds}ms\n" +
                   $"🔢 <b>كود الحالة:</b> {(int)response.StatusCode}";
        }
        catch (TaskCanceledException)
        {
            return "⌛ <b>الحالة:</b> انتهى الوقت المحدد (3 ثواني)";
        }
        catch (HttpRequestException ex)
        {
            return $"❌ <b>الحالة:</b> فشل الاتصال\n" +
                   $"📌 <b>التفاصيل:</b> {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"💢 <b>الحالة:</b> خطأ غير متوقع\n" +
                   $"📌 <b>التفاصيل:</b> {ex.Message}";
        }
    }
}
