using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
        
        var replyMarkup = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("النظام") },
            new[] { new KeyboardButton("موقع الجامعة") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "✅ البوت يعمل الآن!\n" +
                  "اختر أحد الخيارات للبدء:",
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("/stop", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = false;
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "⛔ البوت متوقف الآن. أرسل /start لإعادة التشغيل",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
    else if (messageText.Equals("النظام", StringComparison.OrdinalIgnoreCase) && isRunning)
    {
        await TestAndSendResult(botClient, chatId, "http://huc.edu.iq:9596/", "نظام الجامعة", cancellationToken);
    }
    else if (messageText.Equals("موقع الجامعة", StringComparison.OrdinalIgnoreCase) && isRunning)
    {
        await TestAndSendResult(botClient, chatId, "https://huc.edu.iq", "الموقع الرسمي للجامعة", cancellationToken);
    }
    else if (isRunning)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "⚠️ أمر غير معروف\n" +
                  "الأوامر المتاحة:\n" +
                  "/start - بدء التشغيل\n" +
                  "/stop - إيقاف البوت\n" +
                  "أو استخدم الأزرار الظاهرة",
            cancellationToken: cancellationToken);
    }
}

async Task TestAndSendResult(ITelegramBotClient botClient, long chatId, string url, string siteName, CancellationToken cancellationToken)
{
    // إرسال رسالة بدء الاختبار
    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"🔄 جاري اختبار اتصال {siteName}...",
        cancellationToken: cancellationToken);

    try
    {
        var result = await NetworkService.TestConnection(url);
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"📊 <b>نتيجة اختبار {siteName}</b>\n" +
                  $"🔗 <b>الرابط:</b> {url}\n" +
                  $"{result}\n" +
                  $"─────────────────────",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"❌ <b>فشل اختبار {siteName}</b>\n" +
                  $"🔗 <b>الرابط:</b> {url}\n" +
                  $"الخطأ: {ex.Message}\n" +
                  $"─────────────────────",
            parseMode: ParseMode.Html,
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
            return "⌛ <b>الحالة:</b> انتهى الوقت المحدد (5 ثواني)";
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
