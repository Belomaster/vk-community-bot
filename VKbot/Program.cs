using System;
using System.Text.RegularExpressions;
using VkNet;
using VkNet.Model;
using VkNet.Enums;
using System.Threading;
using Newtonsoft.Json.Linq;
using VkNet.Exception;

namespace VKbot
{
    class Program
    {
        private static readonly VkApi api = new VkApi();
        private static readonly string TOKEN = "vk1.a.waGosV8qs3kW5inMFVV9BZVl3OKtAguORBXytZ9gGDfe_44RWKg1GVlEToS5baO0UbORveYfvywrF5s5rNHYLMeolG80Dc3yF5Y6UJTYvigu4CJJ38U4DiJOZyctAZUK_W5ydAAOO5EwLJwEBBrRu66E3T3v0-cktlV3jffvpXyn7oihwGUw5bcsl0hNCTBcOW0waIOI2mX6a4H6N3YdhQ";
        private static readonly ulong GROUP_ID = 236391205;
        private static readonly Random _random = new Random(); // Статический экземпляр Random для генерации уникальных ID

        static void Main()
        {
            Console.WriteLine("🚀 VK Education Bot");

            if (!Auth())
            {
                Console.ReadKey();
                return;
            }

            Console.WriteLine("✅ vk.com/club236391205 - Тестируйте!");
            Console.WriteLine("📝 привет, /projects, можно несколько проектов?");

            while (true)
            {
                try
                {
                    ProcessUpdates();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка в основном цикле: {ex.Message}");
                }
                Thread.Sleep(1000);
            }
        }

        static bool Auth()
        {
            try
            {
                api.Authorize(new VkNet.Model.ApiAuthParams
                {
                    AccessToken = TOKEN
                });
                Console.WriteLine("✅ Авторизация OK");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
                return false;
            }
        }

        static void ProcessUpdates()
        {
            try
            {
                var server = api.Groups.GetLongPollServer(GROUP_ID);
                var updates = api.Groups.GetBotsLongPollHistory(new VkNet.Model.BotsLongPollHistoryParams
                {
                    Server = server.Server,
                    Ts = server.Ts,
                    Key = server.Key,
                    Wait = 25
                });

                if (updates?.Updates == null) return;

                foreach (var update in updates.Updates)
                {
                    if (update.Type?.ToString() == "MessageNew")
                    {
                        HandleMessage(update);
                    }
                }
            }
            catch (VkApiException ex) when (ex.Message.Contains("longpoll"))
            {
                Console.WriteLine($"❌ Long Poll не активирован для группы. Проверьте настройки группы ВК.");
                Thread.Sleep(5000); // Пауза перед повторной попыткой
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Thread.Sleep(1000);
            }
        }

        static void HandleMessage(VkNet.Model.GroupUpdate update)
        {
            try
            {
                // Безопасный парсинг JSON через JObject
                var json = JObject.FromObject(update);
                var messageData = json["object"]?["message"];

                if (messageData == null) return;

                // Безопасное извлечение peer_id с проверкой на null
                long? peerIdNullable = messageData["peer_id"]?.ToObject<long?>();
                if (!peerIdNullable.HasValue) return;
                long peerId = peerIdNullable.Value;

                string text = messageData["text"]?.ToString()?.Trim().ToLower() ?? "";
                if (peerId <= 0 || string.IsNullOrEmpty(text)) return;

                Console.WriteLine($"📨 [{peerId}] {text}");

                // ✅ ФИЛЬТР МАТА
                if (IsProfanity(text))
                {
                    SendMessage(peerId, "⚠️ Некорректные высказывания запрещены!");
                    return;
                }

                // ✅ FAQ + ДА/НЕТ
                string answer = GetAnswer(text);
                SendMessage(peerId, answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки сообщения: {ex.Message}");
            }
        }

        static bool IsProfanity(string text)
        {
            string pattern = @"ху[йи]|пизд[аы]|бл[яь]д[ь]";
            return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
        }

        static string GetAnswer(string text)
        {
            // ✅ ДА/НЕТ (ТРЕБОВАНИЕ)
            if (text.Contains("несколько проектов") || text.Contains("можно несколько"))
                return "✅ ДА, можно взять несколько проектов!";

            // ✅ FAQ (ТРЕБОВАНИЕ)
            if (text == "привет" || text == "/start")
                return "👋 VK Education Bot\n\n📋 /help - команды";

            if (text == "/help" || text.Contains("помощь"))
                return "📋 Команды:\n• /projects - проекты\n• /выбрать - выбор\n• /файл - файлы\n• /вебинары";

            if (text.Contains("проекты"))
                return "📚 https://education.vk.company/education_projects\n50+ кейсов!";

            if (text.Contains("выбрать"))
                return "✅ 1. Регистрация\n2. Направление\n3. Проект";

            if (text.Contains("файл"))
                return "📎 Загрузка в форму платформы";

            if (text.Contains("вебинар"))
                return "📺 https://education.vk.company";

            return "❓ /help\n👉 https://education.vk.company";
        }

        static void SendMessage(long peerId, string message)
        {
            try
            {
                api.Messages.Send(new MessagesSendParams
                {
                    PeerId = peerId,
                    Message = message,
                    RandomId = _random.Next(1, 100000) // Используем статический Random
                });
                Console.WriteLine($"📤 [{peerId}] OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }
        }
    }
}
