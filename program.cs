using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace DiscordTools
{
    class Program
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly object _consoleLock = new object(); // For thread-safe console writes

        static async Task Main(string[] args)
        {
            Console.Title = "Discord Tool Suite";
            Console.OutputEncoding = Encoding.UTF8; // Set encoding for proper character display

            Console.CancelKeyPress += (sender, e) =>
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("\nCtrl+C detected.  Exiting...");
                }
                _cts.Cancel();
                e.Cancel = true;
            };

            while (true)
            {
                Console.Clear();
                Menu();
                Console.Write("Enter your choice: ");
                string choice = Console.ReadLine();
                try
                {
                    switch (choice)
                    {
                        case "1":
                            await WebhookMessage();
                            break;
                        case "2":
                            await WebhookSpammer();
                            break;
                        case "3":
                            await WebhookDeleter();
                            break;
                        case "4":
                            await WebhookInfo();
                            break;
                        case "5":
                            await ChannelSpammerUserToken();
                            break;
                        case "6":
                            await IpGeolocator();
                            break;
                        case "7":
                            await DeleteAllMessages();
                            break;
                        case "8":
                            await IpPinger();
                            break;
                        case "9":
                            NitroGiftGen();
                            break;
                        case "10":
                            await TokenInfo();
                            break;
                        case "11":
                            await ServerInfo();
                            break;
                        case "12":
                            await Nuker();
                            break;
                        case "13":
                            ShowQRCodeGeneratorLink();
                            break;
                        case "14":
                            Base64EncoderDecoder();
                            break;
                        case "15":
                            TimezoneConverter();
                            break;
                        case "16":
                            await SeeDeletedMessages();
                            break;
                        case "17":
                            lock (_consoleLock)
                            {
                                Console.WriteLine("Exiting application...");
                            }
                            _cts.Cancel();
                            return;
                        default:
                            lock (_consoleLock)
                            {
                                Console.WriteLine("Invalid choice. Please try again.");
                            }
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine("Operation was cancelled.");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    }
                }
                lock (_consoleLock)
                {
                    Console.WriteLine("Press any key to continue...");
                }
                Console.ReadKey();
            }
        }

        static void Menu()
        {
            Console.WriteLine(@"
██████╗  ██████╗ ████████╗ ██████╗  ██████╗ ██╗      
██╔══██╗██╔════╝ ╚══██╔══╝██╔═══██╗██╔═══██╗██║      
██║  ██║██║         ██║   ██║   ██║██║   ██║██║      
██║  ██║██║         ██║   ██║   ██║██║   ██║██║      
██████╔╝╚██████╗    ██║   ╚██████╔╝╚██████╔╝███████╗
╚═════╝  ╚═════╝    ╚═╝    ╚═════╝  ╚═════╝ ╚══════╝
                                  Made By squishyguy232    

1. Send Webhook Message
2. Webhook Spammer
3. Webhook Deleter
4. Webhook Information
5. Channel Spammer (User Token)
6. IP Geolocator
7. Delete All Messages (User Token)
8. IP Pinger
9. Nitro Gift Gen
10. Token Info
11. Server Info
12. Nuker
13. QR Code Generator Link
14. Base64 Encoder/Decoder
15. Timezone Converter
16. See Deleted Messages
17. Exit
");
        }

        static async Task SeeDeletedMessages()
        {
            Console.Clear();
            Console.Write("Enter your User Token: ");
            string token = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(token))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Token cannot be empty. Returning to menu.");
                }
                return;
            }

            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DeletedMessages");
            Directory.CreateDirectory(folderPath);

            lock (_consoleLock)
            {
                Console.WriteLine("Monitoring for deleted messages in DMs and group DMs... Running in background.");
                Console.WriteLine("Press any key to return to menu (monitoring continues until exit).");
            }

            // Start monitoring in the background
            await Task.Run(() => MonitorDeletedMessages(token, folderPath, _cts.Token), _cts.Token);

            // Allow returning to menu while monitoring continues
            Console.ReadKey();
            _cts.Cancel();
        }

        static async Task MonitorDeletedMessages(string token, string folderPath, CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                var lastMessages = new Dictionary<string, List<JObject>>();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Fetch DM channels
                        HttpResponseMessage dmResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/channels", cancellationToken);
                        if (!dmResponse.IsSuccessStatusCode)
                        {
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Failed to fetch DM channels. Status Code: {dmResponse.StatusCode}");
                            }
                            await Task.Delay(5000, cancellationToken);
                            continue;
                        }

                        string dmBody = await dmResponse.Content.ReadAsStringAsync();
                        JArray dmChannels = JArray.Parse(dmBody);

                        foreach (var channel in dmChannels)
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            string channelId = channel["id"].ToString();
                            string channelName = channel["type"].ToObject<int>() == 1
                                ? channel["recipients"][0]["username"].ToString()
                                : channel["name"]?.ToString() ?? $"GroupDM_{channelId}";

                            int channelType = channel["type"].ToObject<int>();
                            if (channelType != 1 && channelType != 3) continue;

                            string messagesUrl = $"https://discord.com/api/v10/channels/{channelId}/messages?limit=100"; // Increased limit
                            HttpResponseMessage msgResponse = await client.GetAsync(messagesUrl, cancellationToken);
                            if (!msgResponse.IsSuccessStatusCode)
                            {
                                lock (_consoleLock)
                                {
                                    Console.WriteLine($"Failed to fetch messages for channel {channelId}. Status Code: {msgResponse.StatusCode}");
                                }
                                continue;
                            }

                            string msgBody = await msgResponse.Content.ReadAsStringAsync();
                            JArray messages = JArray.Parse(msgBody);

                            if (!lastMessages.ContainsKey(channelId))
                            {
                                lastMessages[channelId] = messages.Cast<JObject>().ToList();
                                continue;
                            }

                            var previousMessages = lastMessages[channelId];
                            var currentMessageIds = messages.Select(m => m["id"].ToString()).ToHashSet();

                            foreach (var prevMsg in previousMessages)
                            {
                                if (cancellationToken.IsCancellationRequested) return;
                                string msgId = prevMsg["id"].ToString();
                                if (!currentMessageIds.Contains(msgId))
                                {
                                    string author = prevMsg["author"]["username"]?.ToString() ?? "Unknown";
                                    string content = prevMsg["content"]?.ToString() ?? "";
                                    if (string.IsNullOrEmpty(content)) continue;

                                    int index = previousMessages.IndexOf(prevMsg);
                                    string prevMsg1 = (index - 1 >= 0) ? previousMessages[index - 1]["content"]?.ToString() ?? "NA" : "NA";
                                    string prevMsg2 = (index - 2 >= 0) ? previousMessages[index - 2]["content"]?.ToString() ?? "NA" : "NA";

                                    string filePath = Path.Combine(folderPath, $"{channelName}.txt");
                                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {author}\n" +
                                                      $"  Previous 2: {prevMsg2}\n" +
                                                      $"  Previous 1: {prevMsg1}\n" +
                                                      $"  Deleted Message: {content} [DELETED]\n\n";

                                    File.AppendAllText(filePath, logEntry);
                                    lock (_consoleLock)
                                    {
                                        Console.WriteLine($"Deleted message detected in {channelName} from {author}. Logged to {filePath}");
                                    }
                                }
                            }
                            lastMessages[channelId] = messages.Cast<JObject>().ToList();
                        }
                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine("Monitoring for deleted messages stopped.");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(Path.Combine(folderPath, "ErrorLog.txt"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\n");
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"An error occurred while monitoring deleted messages: {ex.Message}");
                        }
                        await Task.Delay(5000, cancellationToken);
                    }
                }
            }
        }

        static async Task WebhookInfo()
        {
            Console.Clear();
            Console.Write("Enter Webhook URL: ");
            string webhookUrl = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Webhook URL cannot be empty. Returning to menu.");
                }
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(webhookUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        JObject webhookData = JObject.Parse(responseBody);
                        lock (_consoleLock)
                        {
                            Console.WriteLine("\n=== Webhook Information ===");
                            Console.WriteLine($"Webhook ID: {webhookData["id"]}");
                            Console.WriteLine($"Name: {webhookData["name"]}");
                            Console.WriteLine($"Channel ID: {webhookData["channel_id"]}");
                            Console.WriteLine($"Guild ID: {webhookData["guild_id"] ?? "NA"}");
                            Console.WriteLine($"Token: {webhookData["token"]}");
                            Console.WriteLine($"Avatar: {webhookData["avatar"] ?? "None"}");
                            Console.WriteLine($"Application ID: {webhookData["application_id"] ?? "NA"}");
                            Console.WriteLine($"Created At: {webhookData["created_at"] ?? "Unknown"}");
                            Console.WriteLine($"Type: {webhookData["type"]}");
                            Console.WriteLine($"URL: {webhookUrl}");
                        }
                    }
                    else
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to retrieve webhook info. Status: {response.StatusCode}");
                            Console.WriteLine("Note: Discord's API requires the webhook URL to be valid and accessible.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.WriteLine("Make sure you entered a valid Discord webhook URL.");
                    }
                }
            }
        }

        static void NitroGiftGen()
        {
            Console.Clear();
            Console.Write("Enter the number of Nitro Gift codes to generate: ");
            if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid number. Press any key to return to the menu...");
                }
                Console.ReadKey();
                return;
            }
            const string baseUrl = "https://discord.gift/";
            const int codeLength = 16;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            lock (_consoleLock)
            {
                Console.WriteLine();
                for (int i = 0; i < count; i++)
                {
                    string code = new string(Enumerable.Repeat(chars, codeLength)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
                    string giftUrl = baseUrl + code;
                    Console.WriteLine($"[{i + 1}] {giftUrl}");
                }
            }
        }

        static async Task WebhookMessage()
        {
            Console.Clear();
            Console.Write("Webhook URL: ");
            string webhook = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(webhook))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Webhook URL cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Message: ");
            string message = Console.ReadLine();
            if (message == null)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Message cannot be null. Returning to menu.");
                }
                return;
            }
            string json = $"{{\"content\":\"{message}\"}}";
            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = await client.PostAsync(webhook, content);
                    if (response.IsSuccessStatusCode)
                        lock (_consoleLock)
                        {
                            Console.WriteLine("Message sent successfully.");
                        }
                    else
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to send message. HTTP Status: {response.StatusCode}");
                        }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        static async Task WebhookSpammer()
        {
            Console.Clear();
            Console.Write("Webhook URL: ");
            string webhook = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(webhook))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Webhook URL cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Message: ");
            string message = Console.ReadLine();
            if (message == null)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Message cannot be null. Returning to menu.");
                }
                return;
            }
            Console.Write("Interval in seconds (e.g., 0.1): ");
            if (!double.TryParse(Console.ReadLine(), out double intervalSeconds) || intervalSeconds <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid interval. Must be greater than 0. Returning to menu.");
                }
                return;
            }
            TimeSpan delay = TimeSpan.FromSeconds(intervalSeconds);
            string json = $"{{\"content\":\"{message}\"}}";
            lock (_consoleLock)
            {
                Console.WriteLine("Spamming... Press Enter to stop.");
            }
            Task stopTask = Task.Run(Console.ReadLine);
            using (var client = new HttpClient())
            {
                while (!stopTask.IsCompleted)
                {
                    try
                    {
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        await client.PostAsync(webhook, content);
                    }
                    catch (Exception ex)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Error sending message: {ex.Message}");
                        }
                    }
                    await Task.Delay(delay);
                }
            }
            lock (_consoleLock)
            {
                Console.WriteLine("Spamming stopped.");
            }
        }

        static async Task WebhookDeleter()
        {
            Console.Clear();
            Console.Write("Webhook URL: ");
            string webhook = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(webhook))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Webhook URL cannot be empty. Returning to menu.");
                }
                return;
            }
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.DeleteAsync(webhook);
                    if (response.IsSuccessStatusCode)
                        lock (_consoleLock)
                        {
                            Console.WriteLine("Webhook deleted successfully.");
                        }
                    else
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to delete webhook. HTTP Status: {response.StatusCode}");
                        }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        static async Task ChannelSpammerUserToken()
        {
            Console.Clear();
            Console.Write("Enter your User Token: ");
            string token = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(token))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Token cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter Channel ID: ");
            string channelId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Channel ID cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter Message to spam: ");
            string message = Console.ReadLine();
            if (message == null)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Message cannot be null. Returning to menu.");
                }
                return;
            }

            Console.Write("Enter number of times to spam: ");
            if (!int.TryParse(Console.ReadLine(), out int times) || times <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid number. Must be greater than 0. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter interval in seconds (e.g., 0.1): ");
            if (!double.TryParse(Console.ReadLine(), out double intervalSeconds) || intervalSeconds <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid interval. Must be greater than 0. Returning to menu.");
                }
                return;
            }
            TimeSpan delay = TimeSpan.FromSeconds(intervalSeconds);
            string url = $"https://discord.com/api/v10/channels/{channelId}/messages";
            string json = $"{{\"content\":\"{message}\"}}";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                for (int i = 0; i < times; i++)
                {
                    try
                    {
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PostAsync(url, content);
                        if (!response.IsSuccessStatusCode)
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Attempt {i + 1} Failed (HTTP {response.StatusCode})");
                            }
                        else
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Attempt {i + 1} Message sent.");
                            }
                    }
                    catch (Exception ex)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Attempt {i + 1} Exception - {ex.Message}");
                        }
                    }
                    await Task.Delay(delay);
                }
            }
            lock (_consoleLock)
            {
                Console.WriteLine("Spamming complete.");
            }
        }

        static async Task IpGeolocator()
        {
            Console.Clear();
            Console.Write("Enter IP Address: ");
            string ipAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("IP Address cannot be empty. Returning to menu.");
                }
                return;
            }
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"http://ip-api.com/json/{ipAddress}");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        JObject geoData = JObject.Parse(responseBody);
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"IP: {geoData["query"]}");
                            Console.WriteLine($"City: {geoData["city"]}");
                            Console.WriteLine($"Region: {geoData["regionName"]}");
                            Console.WriteLine($"Country: {geoData["country"]}");
                            Console.WriteLine($"Lat: {geoData["lat"]}, Lon: {geoData["lon"]}");
                            Console.WriteLine($"ISP: {geoData["isp"]}");
                        }
                    }
                    else
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to get geolocation data. HTTP Status: {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        static async Task DeleteAllMessages()
        {
            Console.Clear();
            Console.Write("Enter your User Token: ");
            string token = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(token))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Token cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter your User ID: ");
            string userId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userId))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("User ID cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter Channel ID: ");
            string channelId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Channel ID cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter the number of messages to delete (or type 'all' to delete all): ");
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Input cannot be empty. Returning to menu.");
                }
                return;
            }
            bool deleteAll = input.Equals("all", StringComparison.OrdinalIgnoreCase);
            int messagesToDelete = 0;
            if (!deleteAll && !int.TryParse(input, out messagesToDelete))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid input. Please enter a valid number or 'all'.");
                }
                return;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                string baseUrl = $"https://discord.com/api/v10/channels/{channelId}/messages";
                int deletedCount = 0;
                string lastId = null;
                while (true)
                {
                    string url = baseUrl + "?limit=100";
                    if (!string.IsNullOrEmpty(lastId))
                        url += $"&before={lastId}";
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to fetch messages. HTTP Status: {response.StatusCode}");
                        }
                        break;
                    }
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JArray messages = JArray.Parse(responseBody);
                    if (messages.Count == 0)
                        break;
                    foreach (var msg in messages)
                    {
                        string authorId = msg["author"]["id"].ToString();
                        if (authorId != userId)
                            continue;
                        string messageId = msg["id"].ToString();
                        string deleteUrl = $"{baseUrl}/{messageId}";
                        HttpResponseMessage deleteResponse = await client.DeleteAsync(deleteUrl);
                        if (deleteResponse.IsSuccessStatusCode)
                        {
                            deletedCount++;
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Deleted message {messageId}.");
                            }
                        }
                        else
                        {
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Failed to delete message {messageId}. HTTP Status: {deleteResponse.StatusCode}");
                            }
                        }
                        if (!deleteAll && deletedCount >= messagesToDelete)
                            break;
                        await Task.Delay(1000);
                    }
                    if (!deleteAll && deletedCount >= messagesToDelete)
                        break;
                    lastId = messages.Last["id"].ToString();
                    if (string.IsNullOrEmpty(lastId))
                        break;
                }
                lock (_consoleLock)
                {
                    if (deletedCount == 0)
                        Console.WriteLine("No messages found to delete.");
                    else
                        Console.WriteLine($"Total messages deleted: {deletedCount}");
                }
            }
        }

        static async Task IpPinger()
        {
            Console.Clear();
            Console.Write("Enter IP Address to ping: ");
            string ipAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("IP Address cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter how fast to ping (in seconds, e.g., 0.1): ");
            if (!double.TryParse(Console.ReadLine(), out double intervalSeconds) || intervalSeconds <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid interval. Must be greater than 0. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter number of times to ping: ");
            if (!int.TryParse(Console.ReadLine(), out int pingCount) || pingCount <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid number. Must be greater than 0. Returning to menu.");
                }
                return;
            }
            TimeSpan delay = TimeSpan.FromSeconds(intervalSeconds);
            lock (_consoleLock)
            {
                Console.WriteLine($"Pinging {ipAddress}...");
            }
            using (var pingSender = new Ping())
            {
                for (int i = 0; i < pingCount; i++)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        PingReply reply = await pingSender.SendPingAsync(ipAddress);
                        stopwatch.Stop();
                        lock (_consoleLock)
                        {
                            if (reply.Status == IPStatus.Success)
                                Console.WriteLine($"Ping {i + 1}: {reply.RoundtripTime} ms");
                            else
                                Console.WriteLine($"Ping {i + 1}: Failed - {reply.Status}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Ping {i + 1}: Error - {ex.Message}");
                        }
                    }
                    await Task.Delay(delay);
                }
            }
        }

        static async Task TokenInfo()
        {
            Console.Clear();
            Console.Write("Enter your Discord Token: ");
            string token = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(token))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Token cannot be empty. Returning to menu.");
                }
                return;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                try
                {
                    HttpResponseMessage userResponse = await client.GetAsync("https://discord.com/api/v10/users/@me");
                    if (!userResponse.IsSuccessStatusCode)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to retrieve user info. HTTP Status: {userResponse.StatusCode}");
                        }
                        return;
                    }
                    string userBody = await userResponse.Content.ReadAsStringAsync();
                    JObject userData = JObject.Parse(userBody);
                    string id = userData["id"].ToString();
                    string username = $"{userData["username"]}#{userData["discriminator"]}";
                    string email = userData["email"]?.ToString() ?? "NA";
                    bool emailVerified = userData["verified"].ToObject<bool>();
                    string phone = userData["phone"]?.ToString() ?? "NA";
                    bool mfaEnabled = userData["mfa_enabled"].ToObject<bool>();
                    string locale = userData["locale"]?.ToString() ?? "Unknown";
                    int premiumType = userData["premium_type"].ToObject<int>();
                    long snowflake = Convert.ToInt64(id);
                    long discordEpoch = 1420070400000;
                    long timestamp = (snowflake >> 22) + discordEpoch;
                    DateTimeOffset createdAt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    HttpResponseMessage guildsResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/guilds");
                    int guildCount = 0;
                    if (guildsResponse.IsSuccessStatusCode)
                    {
                        string guildsBody = await guildsResponse.Content.ReadAsStringAsync();
                        JArray guildsData = JArray.Parse(guildsBody);
                        guildCount = guildsData.Count;
                    }
                    HttpResponseMessage friendsResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/relationships");
                    int friendsCount = 0;
                    if (friendsResponse.IsSuccessStatusCode)
                    {
                        string friendsBody = await friendsResponse.Content.ReadAsStringAsync();
                        JArray friendsData = JArray.Parse(friendsBody);
                        friendsCount = friendsData.Count(rel => rel["type"].ToObject<int>() == 1);
                    }
                    HttpResponseMessage blockedResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/blocked?limit=100");
                    int blockedCount = 0;
                    if (blockedResponse.IsSuccessStatusCode)
                    {
                        string blockedBody = await blockedResponse.Content.ReadAsStringAsync();
                        JArray blockedData = JArray.Parse(blockedBody);
                        blockedCount = blockedData.Count;
                    }
                    string accountStanding = "Active";
                    double totalMoneySpent = 0.0;
                    try
                    {
                        HttpResponseMessage billingResponse = await client.GetAsync("https://discord.com/api/v9/users/@me/billing/payments");
                        if (billingResponse.IsSuccessStatusCode)
                        {
                            string billingBody = await billingResponse.Content.ReadAsStringAsync();
                            JArray billingData = JArray.Parse(billingBody);
                            foreach (var entry in billingData)
                            {
                                totalMoneySpent += entry["amount"]?.ToObject<double>() / 100.0 ?? 0;
                            }
                        }
                    }
                    catch { }
                    lock (_consoleLock)
                    {
                        Console.WriteLine("\n=== Token Information ===");
                        Console.WriteLine($"Username: {username}");
                        Console.WriteLine($"ID: {id}");
                        Console.WriteLine($"Email: {email}");
                        Console.WriteLine($"Email Verified: {(emailVerified ? "Yes" : "No")}");
                        Console.WriteLine($"Phone: {phone}");
                        Console.WriteLine($"MFA Enabled: {(mfaEnabled ? "Yes" : "No")}");
                        Console.WriteLine($"Account Standing: {accountStanding}");
                        Console.WriteLine($"Account Created: {createdAt.LocalDateTime}");
                        Console.WriteLine($"Locale: {locale}");
                        Console.WriteLine($"Nitro: {(premiumType != 0 ? "Yes" : "No")}");
                        Console.WriteLine($"Server Count: {guildCount}");
                        Console.WriteLine($"Friends Count: {friendsCount}");
                        Console.WriteLine($"Blocked Users: {blockedCount}");
                        Console.WriteLine($"Money Spent: ${totalMoneySpent}");
                    }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error retrieving token info: {ex.Message}");
                    }
                }
            }
        }

        static async Task ServerInfo()
        {
            Console.Clear();
            Console.Write("Enter your Discord Token (Bot/User token with access to the server): ");
            string token = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(token))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Token cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter Server (Guild) ID: ");
            string guildId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(guildId))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Server (Guild) ID cannot be empty. Returning to menu.");
                }
                return;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                try
                {
                    HttpResponseMessage guildResponse = await client.GetAsync($"https://discord.com/api/v10/guilds/{guildId}?with_counts=true");
                    if (!guildResponse.IsSuccessStatusCode)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to retrieve server info. HTTP Status: {guildResponse.StatusCode}");
                        }
                        return;
                    }
                    string guildBody = await guildResponse.Content.ReadAsStringAsync();
                    JObject guildData = JObject.Parse(guildBody);
                    long snowflake = Convert.ToInt64(guildData["id"].ToString());
                    long discordEpoch = 1420070400000;
                    long timestamp = (snowflake >> 22) + discordEpoch;
                    DateTimeOffset createdAt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    string memberCount = guildData["approximate_member_count"]?.ToString() ?? "Unknown";
                    HttpResponseMessage stickersResponse = await client.GetAsync($"https://discord.com/api/v10/guilds/{guildId}/stickers");
                    int stickersCount = 0;
                    if (stickersResponse.IsSuccessStatusCode)
                    {
                        string stickersBody = await stickersResponse.Content.ReadAsStringAsync();
                        JArray stickersData = JArray.Parse(stickersBody);
                        stickersCount = stickersData.Count;
                    }
                    lock (_consoleLock)
                    {
                        Console.WriteLine("\n=== Server Information ===");
                        Console.WriteLine($"Server Name: {guildData["name"]}");
                        Console.WriteLine($"Member Count: {memberCount}");
                        Console.WriteLine($"Server Created: {createdAt.LocalDateTime}");
                        Console.WriteLine($"Custom Stickers: {stickersCount}");
                    }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error retrieving server info: {ex.Message}");
                    }
                }
            }
        }

        static void ShowQRCodeGeneratorLink()
        {
            Console.Clear();
            string link = "https://www.qr-code-generator.com";
            lock (_consoleLock)
            {
                Console.WriteLine("QR Code Generator");
                Console.WriteLine(link);
            }
        }

        static void Base64EncoderDecoder()
        {
            Console.Clear();
            lock (_consoleLock)
            {
                Console.WriteLine("Base64 Encoder/Decoder");
            }
            Console.Write("Enter 1 to Encode or 2 to Decode: ");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.Write("Enter text to encode: ");
                string input = Console.ReadLine();
                if (input == null)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine("Input cannot be null. Returning to menu.");
                    }
                    return;
                }
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
                lock (_consoleLock)
                {
                    Console.WriteLine($"Encoded: {encoded}");
                }
            }
            else if (choice == "2")
            {
                Console.Write("Enter Base64 string to decode: ");
                string input = Console.ReadLine();
                if (input == null)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine("Input cannot be null. Returning to menu.");
                    }
                    return;
                }
                try
                {
                    string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(input));
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Decoded: {decoded}");
                    }
                }
                catch (FormatException)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine("Invalid Base64 string.");
                    }
                }
            }
            else
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid choice.");
                }
            }
        }

        static void TimezoneConverter()
        {
            Console.Clear();
            lock (_consoleLock)
            {
                Console.WriteLine("Timezone Converter");
            }
            Console.Write("Enter datetime (yyyy-MM-dd HH:mm:ss): ");
            string dateTimeStr = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(dateTimeStr))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Datetime string cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter source timezone offset (hours, e.g., -5): ");
            string sourceOffsetStr = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(sourceOffsetStr))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Source timezone offset cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter target timezone offset (hours, e.g., 2): ");
            string targetOffsetStr = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(targetOffsetStr))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Target timezone offset cannot be empty. Returning to menu.");
                }
                return;
            }
            try
            {
                DateTime dateTime = DateTime.ParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                double sourceOffset = double.Parse(sourceOffsetStr, CultureInfo.InvariantCulture);
                double targetOffset = double.Parse(targetOffsetStr, CultureInfo.InvariantCulture);
                DateTime utcTime = dateTime.AddHours(-sourceOffset);
                DateTime targetTime = utcTime.AddHours(targetOffset);
                lock (_consoleLock)
                {
                    Console.WriteLine($"Converted Time: {targetTime:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static async Task Nuker()
        {
            Console.Clear();
            Console.Write("Enter your User or Bot Token: ");
            string token = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(token))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Token cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter Server (Guild) ID: ");
            string guildId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(guildId))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Server (Guild) ID cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter the number of channels to create: ");
            if (!int.TryParse(Console.ReadLine(), out int channelCount) || channelCount <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid number of channels.");
                }
                return;
            }
            Console.Write("Enter the custom channel name (base): ");
            string channelNameBase = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(channelNameBase))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Channel name base cannot be empty. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter the message to spam in each channel: ");
            string message = Console.ReadLine();
            if (message == null)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Message cannot be null. Returning to menu.");
                }
                return;
            }
            Console.Write("Enter the number of messages to spam in each channel: ");
            if (!int.TryParse(Console.ReadLine(), out int msgCount) || msgCount <= 0)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("Invalid number of messages.");
                }
                return;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                try
                {
                    // 1. Get Guild Channels
                    HttpResponseMessage channelsResponse = await client.GetAsync($"https://discord.com/api/v10/guilds/{guildId}/channels");
                    if (!channelsResponse.IsSuccessStatusCode)
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"Failed to get channels: {channelsResponse.StatusCode}");
                        }
                        return;
                    }
                    string channelsJson = await channelsResponse.Content.ReadAsStringAsync();
                    JArray channels = JArray.Parse(channelsJson);

                    // 2. Delete Existing Channels
                    foreach (var channel in channels)
                    {
                        string channelId = channel["id"].ToString();
                        HttpResponseMessage deleteResponse = await client.DeleteAsync($"https://discord.com/api/v10/channels/{channelId}");
                        if (deleteResponse.IsSuccessStatusCode)
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Deleted channel: {channelId}");
                            }
                        else
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Failed to delete channel {channelId} - {deleteResponse.StatusCode}");
                            }
                    }

                    // 3. Create New Channels and Spam
                    for (int i = 0; i < channelCount; i++)
                    {
                        string channelName = $"{channelNameBase}-{i + 1}";
                        string json = $"{{\"name\":\"{channelName}\",\"type\":0}}";
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        HttpResponseMessage createResponse = await client.PostAsync($"https://discord.com/api/v10/guilds/{guildId}/channels", content);
                        if (createResponse.IsSuccessStatusCode)
                        {
                            string newChannelJson = await createResponse.Content.ReadAsStringAsync();
                            JObject newChannelData = JObject.Parse(newChannelJson);
                            string newChannelId = newChannelData["id"].ToString();
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Created channel: {channelName}");
                            }

                            // Spam messages in the new channel
                            string spamUrl = $"https://discord.com/api/v10/channels/{newChannelId}/messages";
                            string spamJson = $"{{\"content\":\"{message}\"}}";
                            var spamContent = new StringContent(spamJson, Encoding.UTF8, "application/json");
                            for (int j = 0; j < msgCount; j++)
                            {
                                HttpResponseMessage spamResponse = await client.PostAsync(spamUrl, spamContent);
                                if (spamResponse.IsSuccessStatusCode)
                                    lock (_consoleLock)
                                    {
                                        Console.WriteLine($"  [{channelName}] Sent message {j + 1}/{msgCount}");
                                    }
                                else
                                    lock (_consoleLock)
                                    {
                                        Console.WriteLine($"  [{channelName}] Failed to send message {j + 1}/{msgCount} - {spamResponse.StatusCode}");
                                    }
                                await Task.Delay(500);
                            }
                        }
                        else
                            lock (_consoleLock)
                            {
                                Console.WriteLine($"Failed to create channel {channelName} - {createResponse.StatusCode}");
                            }
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"Error during nuking: {ex.Message}");
                    }
                }
            }
        }
    }
}
