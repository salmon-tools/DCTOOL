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
    /// <summary>
    /// A static class dedicated to rendering all UI components with a consistent, modern style.
    /// Handles colors, borders, and structured layouts for a more appealing console experience.
    /// </summary>
    public static class UI
    {
        private static readonly object _consoleLock = new object(); // Use a shared lock object

        // --- Theme Colors ---
        private const ConsoleColor PrimaryColor = ConsoleColor.Cyan;
        private const ConsoleColor SecondaryColor = ConsoleColor.Magenta;
        private const ConsoleColor SuccessColor = ConsoleColor.Green;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor WarningColor = ConsoleColor.Yellow;
        private const ConsoleColor MutedColor = ConsoleColor.DarkGray;
        private const ConsoleColor TextColor = ConsoleColor.Gray;
        private const ConsoleColor ValueColor = ConsoleColor.White;

        /// <summary>
        /// Draws the main application header and menu.
        /// </summary>
        public static void DisplayMenu()
        {
            lock (_consoleLock)
            {
                Console.Clear();
                string title = "Discord Tool Suite";
                string author = "Made By the team at salmon tools";
                string[] menuItems = {
                    "Send Webhook Message", "Webhook Spammer", "Webhook Deleter",
                    "Webhook Information", "Channel Spammer (User Token)", "IP Geolocator",
                    "Delete All Messages (User Token)", "IP Pinger", "Nitro Gift Gen",
                    "Token Info", "Server Info", "Nuker", "QR Code Generator Link",
                    "Base64 Encoder/Decoder", "Timezone Converter", "See Deleted Messages",
                    "Change User Token", "Exit"
                };

                // Draw ASCII Art
                Console.ForegroundColor = PrimaryColor;
                Console.WriteLine(@"
██████╗  ██████╗ ████████╗ ██████╗  ██████╗ ██╗      
██╔══██╗██╔════╝ ╚══██╔══╝██╔═══██╗██╔═══██╗██║      
██║  ██║██║         ██║   ██║   ██║██║   ██║██║      
██║  ██║██║         ██║   ██║   ██║██║   ██║██║      
██████╔╝╚██████╗    ██║   ╚██████╔╝╚██████╔╝███████╗
╚═════╝  ╚═════╝    ╚═╝    ╚═════╝  ╚═════╝ ╚══════╝
");
                Console.ForegroundColor = MutedColor;
                Console.WriteLine(author.PadLeft(Console.WindowWidth / 2 + author.Length / 2));
                Console.WriteLine();

                // Draw Menu Items in two columns
                int maxItemsPerColumn = (menuItems.Length + 1) / 2;
                for (int i = 0; i < maxItemsPerColumn; i++)
                {
                    string leftItem = $"{i + 1}. {menuItems[i]}";
                    string rightItem = "";
                    if (i + maxItemsPerColumn < menuItems.Length)
                    {
                        rightItem = $"{i + maxItemsPerColumn + 1}. {menuItems[i + maxItemsPerColumn]}";
                    }

                    Console.ForegroundColor = WarningColor;
                    Console.Write($"  {i + 1,2}. ");
                    Console.ForegroundColor = TextColor;
                    Console.Write(menuItems[i].PadRight(35));

                    if (!string.IsNullOrEmpty(rightItem))
                    {
                        Console.ForegroundColor = WarningColor;
                        Console.Write($"{i + maxItemsPerColumn + 1,2}. ");
                        Console.ForegroundColor = TextColor;
                        Console.WriteLine(menuItems[i + maxItemsPerColumn]);
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Prints a styled header for a specific tool.
        /// </summary>
        public static void PrintHeader(string title)
        {
            lock (_consoleLock)
            {
                Console.Clear();
                Console.ForegroundColor = PrimaryColor;
                Console.WriteLine($"\n╔{"═".PadRight(title.Length + 2, '═')}╗");
                Console.WriteLine($"║ {title.ToUpper()} ║");
                Console.WriteLine($"╚{"═".PadRight(title.Length + 2, '═')}╝\n");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Displays a prompt and reads user input.
        /// </summary>
        public static string GetInput(string prompt)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = PrimaryColor;
                Console.Write($"\n► {prompt}: ");
                Console.ForegroundColor = ValueColor;
                string input = Console.ReadLine();
                Console.ResetColor();
                return input;
            }
        }

        /// <summary>
        /// Displays a key-value pair for informational screens.
        /// </summary>
        public static void PrintInfo(string key, object value)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = TextColor;
                Console.Write($"  {key,-20}: ");
                Console.ForegroundColor = ValueColor;
                Console.WriteLine(value);
                Console.ResetColor();
            }
        }

        public static void PrintSuccess(string message) => PrintMessage(message, SuccessColor);
        public static void PrintError(string message) => PrintMessage(message, ErrorColor);
        public static void PrintWarning(string message) => PrintMessage(message, WarningColor);
        public static void PrintMessage(string message, ConsoleColor? color = null)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = color ?? TextColor;
                Console.WriteLine($"  {message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Waits for user confirmation to continue.
        /// </summary>
        public static void Wait()
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = MutedColor;
                Console.WriteLine("\nPress any key to return to the menu...");
                Console.ResetColor();
            }
            Console.ReadKey();
        }
    }


    /// <summary>
    /// Manages loading, saving, and requesting the user's Discord token.
    /// </summary>
    public static class TokenManager
    {
        // The file name has been changed from "token.config" to "token.txt"
        private const string TokenFilePath = "token.txt";

        /// <summary>
        /// Gets the token from the config file. If the file doesn't exist or is empty,
        /// it prompts the user to enter and save a new token.
        /// </summary>
        /// <returns>The user's Discord token.</returns>
        public static string GetOrRequestToken()
        {
            if (File.Exists(TokenFilePath))
            {
                string token = File.ReadAllText(TokenFilePath).Trim();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token; // Return the saved token if it's valid
                }
            }

            // If file doesn't exist, is empty, or contains only whitespace, prompt the user.
            return PromptAndSaveToken(isFirstTime: true);
        }

        /// <summary>
        /// Prompts the user to enter a new token and saves it to the config file.
        /// Used for explicitly changing the token from the menu.
        /// </summary>
        /// <returns>The newly entered Discord token.</returns>
        public static string ChangeToken()
        {
            UI.PrintHeader("Change Discord Token");
            UI.PrintWarning("You are about to change the saved Discord token.");
            return PromptAndSaveToken(isFirstTime: false);
        }

        /// <summary>
        /// Handles the logic for prompting the user for a token and saving it to the file.
        /// </summary>
        /// <param name="isFirstTime">If true, displays a welcome message.</param>
        /// <returns>The entered token.</returns>
        private static string PromptAndSaveToken(bool isFirstTime)
        {
            if (isFirstTime)
            {
                UI.PrintHeader("Welcome to Discord Tool Suite");
                UI.PrintMessage("To use token-based features, please provide your Discord token.");
                UI.PrintMessage($"This will be saved locally in a '{TokenFilePath}' file for future sessions.");
            }

            string token;
            do
            {
                token = UI.GetInput("Please enter your Discord User Token");
                if (string.IsNullOrWhiteSpace(token))
                {
                    UI.PrintError("Token cannot be empty. Please try again.");
                }
            } while (string.IsNullOrWhiteSpace(token));

            File.WriteAllText(TokenFilePath, token);
            UI.PrintSuccess("\nToken saved successfully!");
            UI.PrintMessage($"You can change it later by editing '{TokenFilePath}' or using the menu option.");
            UI.Wait();
            return token;
        }
    }


    class Program
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly object _consoleLock = new object(); // For thread-safe console writes
        private static string _userToken; // Holds the user token for the current session

        static async Task Main(string[] args)
        {
            Console.Title = "Discord Tool Suite";
            Console.OutputEncoding = Encoding.UTF8; // Set encoding for proper character display

            // Get or request the user token at startup.
            _userToken = TokenManager.GetOrRequestToken();

            Console.CancelKeyPress += (sender, e) =>
            {
                lock (_consoleLock)
                {
                    UI.PrintError("\nCtrl+C detected. Exiting...");
                }
                _cts.Cancel();
                e.Cancel = true;
            };

            while (!_cts.IsCancellationRequested)
            {
                UI.DisplayMenu();
                string choice = UI.GetInput("Enter your choice");
                try
                {
                    switch (choice)
                    {
                        case "1": await WebhookMessage(); break;
                        case "2": await WebhookSpammer(); break;
                        case "3": await WebhookDeleter(); break;
                        case "4": await WebhookInfo(); break;
                        case "5": await RunWithTokenCheckAsync(_userToken, ChannelSpammerUserToken); break;
                        case "6": await IpGeolocator(); break;
                        case "7": await RunWithTokenCheckAsync(_userToken, DeleteAllMessages); break;
                        case "8": await IpPinger(); break;
                        case "9": NitroGiftGen(); break;
                        case "10": await RunWithTokenCheckAsync(_userToken, TokenInfo); break;
                        case "11": await RunWithTokenCheckAsync(_userToken, ServerInfo); break;
                        case "12": await RunWithTokenCheckAsync(_userToken, Nuker); break;
                        case "13": ShowQRCodeGeneratorLink(); break;
                        case "14": Base64EncoderDecoder(); break;
                        case "15": TimezoneConverter(); break;
                        case "16": await RunWithTokenCheckAsync(_userToken, SeeDeletedMessages); break;
                        case "17": _userToken = TokenManager.ChangeToken(); break;
                        case "18":
                            UI.PrintMessage("Exiting application...");
                            _cts.Cancel();
                            return;
                        default:
                            UI.PrintError("Invalid choice. Please try again.");
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    UI.PrintWarning("Operation was cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    UI.PrintError($"An unexpected error occurred: {ex.Message}");
                }

                if (choice != "17" && choice != "18" && !_cts.IsCancellationRequested)
                {
                    UI.Wait();
                }
            }
        }

        private static async Task RunWithTokenCheckAsync(string token, Func<string, Task> asyncAction)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                UI.PrintHeader("Token Required");
                UI.PrintError("No token provided. Please use option '17' to set a token first.");
                return;
            }
            await asyncAction(token);
        }

        // --- All methods below are refactored to use the UI class ---

        static async Task SeeDeletedMessages(string token)
        {
            UI.PrintHeader("See Deleted Messages");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DeletedMessages");
            Directory.CreateDirectory(folderPath);

            UI.PrintMessage("Monitoring for deleted messages in DMs and group DMs... Running in background.");
            UI.PrintWarning("Press any key to return to menu (monitoring continues until exit).");

            await Task.Run(() => MonitorDeletedMessages(token, folderPath, _cts.Token), _cts.Token);

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
                        HttpResponseMessage dmResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/channels", cancellationToken);
                        if (!dmResponse.IsSuccessStatusCode)
                        {
                            UI.PrintError($"Failed to fetch DM channels. Status Code: {dmResponse.StatusCode}");
                            await Task.Delay(5000, cancellationToken);
                            continue;
                        }

                        JArray dmChannels = JArray.Parse(await dmResponse.Content.ReadAsStringAsync());

                        foreach (var channel in dmChannels)
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            string channelId = channel["id"].ToString();
                            string channelName = channel["type"].ToObject<int>() == 1
                                ? channel["recipients"][0]["username"].ToString()
                                : channel["name"]?.ToString() ?? $"GroupDM_{channelId}";

                            // Process only DMs and Group DMs
                            int channelType = channel["type"].ToObject<int>();
                            if (channelType != 1 && channelType != 3) continue;

                            HttpResponseMessage msgResponse = await client.GetAsync($"https://discord.com/api/v10/channels/{channelId}/messages?limit=100", cancellationToken);
                            if (!msgResponse.IsSuccessStatusCode) continue;

                            JArray messages = JArray.Parse(await msgResponse.Content.ReadAsStringAsync());
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
                                if (!currentMessageIds.Contains(prevMsg["id"].ToString()))
                                {
                                    string author = prevMsg["author"]["username"]?.ToString() ?? "Unknown";
                                    string content = prevMsg["content"]?.ToString() ?? "";
                                    if (string.IsNullOrEmpty(content)) continue;

                                    string filePath = Path.Combine(folderPath, $"{channelName}.txt");
                                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {author}: {content} [DELETED]\n";
                                    File.AppendAllText(filePath, logEntry);

                                    UI.PrintWarning($"Deleted message from {author} in {channelName}: \"{content}\"");
                                }
                            }
                            lastMessages[channelId] = messages.Cast<JObject>().ToList();
                        }
                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        UI.PrintWarning("Monitoring for deleted messages stopped.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(Path.Combine(folderPath, "ErrorLog.txt"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\n");
                        UI.PrintError($"An error occurred while monitoring: {ex.Message}");
                        await Task.Delay(5000, cancellationToken);
                    }
                }
            }
        }

        static async Task ChannelSpammerUserToken(string token)
        {
            UI.PrintHeader("Channel Spammer (User Token)");
            string channelId = UI.GetInput("Enter Channel ID");
            if (string.IsNullOrWhiteSpace(channelId)) { UI.PrintError("Channel ID cannot be empty."); return; }
            string message = UI.GetInput("Enter Message to spam");
            if (string.IsNullOrEmpty(message)) { UI.PrintError("Message cannot be empty."); return; }
            if (!int.TryParse(UI.GetInput("Enter number of times to spam"), out int times) || times <= 0) { UI.PrintError("Invalid number."); return; }
            if (!double.TryParse(UI.GetInput("Enter interval in seconds (e.g., 0.1)"), out double intervalSeconds) || intervalSeconds <= 0) { UI.PrintError("Invalid interval."); return; }

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
                        if (response.IsSuccessStatusCode)
                            UI.PrintSuccess($"[{i + 1}/{times}] Message sent.");
                        else
                            UI.PrintError($"[{i + 1}/{times}] Failed (HTTP {response.StatusCode})");
                    }
                    catch (Exception ex)
                    {
                        UI.PrintError($"[{i + 1}/{times}] Exception - {ex.Message}");
                    }
                    await Task.Delay(delay);
                }
            }
            UI.PrintSuccess("Spamming complete.");
        }

        static async Task DeleteAllMessages(string token)
        {
            UI.PrintHeader("Delete All Messages");
            string userId = UI.GetInput("Enter your User ID");
            if (string.IsNullOrWhiteSpace(userId)) { UI.PrintError("User ID cannot be empty."); return; }
            string channelId = UI.GetInput("Enter Channel ID");
            if (string.IsNullOrWhiteSpace(channelId)) { UI.PrintError("Channel ID cannot be empty."); return; }
            string input = UI.GetInput("Enter the number of messages to delete (or 'all')");
            if (string.IsNullOrWhiteSpace(input)) { UI.PrintError("Input cannot be empty."); return; }

            bool deleteAll = input.Equals("all", StringComparison.OrdinalIgnoreCase);
            if (!deleteAll && !int.TryParse(input, out int _)) { UI.PrintError("Invalid input."); return; }
            int messagesToDelete = deleteAll ? int.MaxValue : int.Parse(input);
            int deletedCount = 0;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                string baseUrl = $"https://discord.com/api/v10/channels/{channelId}/messages";
                string lastId = null;

                while (deletedCount < messagesToDelete)
                {
                    string url = baseUrl + "?limit=100" + (string.IsNullOrEmpty(lastId) ? "" : $"&before={lastId}");
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        UI.PrintError($"Failed to fetch messages. HTTP Status: {response.StatusCode}");
                        break;
                    }
                    JArray messages = JArray.Parse(await response.Content.ReadAsStringAsync());
                    if (messages.Count == 0) break;

                    var userMessages = messages.Where(m => m["author"]["id"].ToString() == userId).ToList();

                    foreach (var msg in userMessages)
                    {
                        string messageId = msg["id"].ToString();
                        HttpResponseMessage deleteResponse = await client.DeleteAsync($"{baseUrl}/{messageId}");
                        if (deleteResponse.IsSuccessStatusCode)
                        {
                            deletedCount++;
                            UI.PrintSuccess($"Deleted message {messageId} ({deletedCount}/{messagesToDelete})");
                        }
                        else
                        {
                            UI.PrintError($"Failed to delete message {messageId}. HTTP Status: {deleteResponse.StatusCode}");
                        }
                        if (deletedCount >= messagesToDelete) break;
                        await Task.Delay(1000); // Rate limit
                    }
                    if (userMessages.Count == 0) break; // No more messages by user in this batch
                    lastId = messages.Last["id"].ToString();
                }
                if (deletedCount == 0) UI.PrintWarning("No messages found to delete.");
                else UI.PrintSuccess($"Total messages deleted: {deletedCount}");
            }
        }

        static async Task TokenInfo(string token)
        {
            UI.PrintHeader("Token Information");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                try
                {
                    HttpResponseMessage userResponse = await client.GetAsync("https://discord.com/api/v10/users/@me");
                    if (!userResponse.IsSuccessStatusCode)
                    {
                        UI.PrintError($"Failed to retrieve user info. HTTP Status: {userResponse.StatusCode}");
                        return;
                    }
                    JObject userData = JObject.Parse(await userResponse.Content.ReadAsStringAsync());
                    string id = userData["id"].ToString();
                    long snowflake = Convert.ToInt64(id);
                    long discordEpoch = 1420070400000;
                    DateTimeOffset createdAt = DateTimeOffset.FromUnixTimeMilliseconds((snowflake >> 22) + discordEpoch);

                    // Fetch supplemental data
                    var guildsResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/guilds");
                    int guildCount = guildsResponse.IsSuccessStatusCode ? JArray.Parse(await guildsResponse.Content.ReadAsStringAsync()).Count : 0;
                    var friendsResponse = await client.GetAsync("https://discord.com/api/v10/users/@me/relationships");
                    int friendsCount = friendsResponse.IsSuccessStatusCode ? JArray.Parse(await friendsResponse.Content.ReadAsStringAsync()).Count(r => r["type"].ToObject<int>() == 1) : 0;

                    // Display Info
                    UI.PrintInfo("Username", $"{userData["username"]}#{userData["discriminator"]}");
                    UI.PrintInfo("ID", id);
                    UI.PrintInfo("Email", userData["email"]?.ToString() ?? "N/A");
                    UI.PrintInfo("Email Verified", (bool)userData["verified"] ? "Yes" : "No");
                    UI.PrintInfo("Phone", userData["phone"]?.ToString() ?? "N/A");
                    UI.PrintInfo("MFA Enabled", (bool)userData["mfa_enabled"] ? "Yes" : "No");
                    UI.PrintInfo("Account Created", createdAt.LocalDateTime);
                    UI.PrintInfo("Locale", userData["locale"]?.ToString() ?? "Unknown");
                    UI.PrintInfo("Nitro", (int)userData["premium_type"] != 0 ? "Yes" : "No");
                    UI.PrintInfo("Server Count", guildCount);
                    UI.PrintInfo("Friends Count", friendsCount);
                }
                catch (Exception ex)
                {
                    UI.PrintError($"Error retrieving token info: {ex.Message}");
                }
            }
        }

        static async Task ServerInfo(string token)
        {
            UI.PrintHeader("Server Information");
            string guildId = UI.GetInput("Enter Server (Guild) ID");
            if (string.IsNullOrWhiteSpace(guildId)) { UI.PrintError("Server ID cannot be empty."); return; }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                try
                {
                    HttpResponseMessage guildResponse = await client.GetAsync($"https://discord.com/api/v10/guilds/{guildId}?with_counts=true");
                    if (!guildResponse.IsSuccessStatusCode)
                    {
                        UI.PrintError($"Failed to retrieve server info. HTTP Status: {guildResponse.StatusCode}");
                        return;
                    }
                    JObject guildData = JObject.Parse(await guildResponse.Content.ReadAsStringAsync());
                    long snowflake = Convert.ToInt64(guildData["id"].ToString());
                    long discordEpoch = 1420070400000;
                    DateTimeOffset createdAt = DateTimeOffset.FromUnixTimeMilliseconds((snowflake >> 22) + discordEpoch);

                    UI.PrintInfo("Server Name", guildData["name"]);
                    UI.PrintInfo("Member Count", guildData["approximate_member_count"] ?? "Unknown");
                    UI.PrintInfo("Server Created", createdAt.LocalDateTime);
                    UI.PrintInfo("Boost Level", guildData["premium_tier"]);
                    UI.PrintInfo("Boosts", guildData["premium_subscription_count"]);
                    UI.PrintInfo("Vanity URL", guildData["vanity_url_code"] ?? "None");
                }
                catch (Exception ex)
                {
                    UI.PrintError($"Error retrieving server info: {ex.Message}");
                }
            }
        }

        static async Task Nuker(string token)
        {
            UI.PrintHeader("Server Nuker");
            string guildId = UI.GetInput("Enter Server (Guild) ID");
            if (string.IsNullOrWhiteSpace(guildId)) { UI.PrintError("Server ID cannot be empty."); return; }
            if (!int.TryParse(UI.GetInput("Enter number of channels to create"), out int channelCount) || channelCount <= 0) { UI.PrintError("Invalid number."); return; }
            string channelNameBase = UI.GetInput("Enter channel name base");
            if (string.IsNullOrWhiteSpace(channelNameBase)) { UI.PrintError("Channel name cannot be empty."); return; }
            string message = UI.GetInput("Enter message to spam");
            if (string.IsNullOrEmpty(message)) { UI.PrintError("Message cannot be empty."); return; }
            if (!int.TryParse(UI.GetInput("Enter number of messages per channel"), out int msgCount) || msgCount <= 0) { UI.PrintError("Invalid number."); return; }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                try
                {
                    // Delete existing channels
                    UI.PrintMessage("\nDeleting existing channels...");
                    var channelsResponse = await client.GetAsync($"https://discord.com/api/v10/guilds/{guildId}/channels");
                    if (channelsResponse.IsSuccessStatusCode)
                    {
                        JArray channels = JArray.Parse(await channelsResponse.Content.ReadAsStringAsync());
                        foreach (var channel in channels)
                        {
                            var delResponse = await client.DeleteAsync($"https://discord.com/api/v10/channels/{channel["id"]}");
                            if (delResponse.IsSuccessStatusCode) UI.PrintMessage($"  Deleted channel {channel["name"]}");
                            else UI.PrintError($"  Failed to delete channel {channel["name"]}");
                        }
                    }

                    // Create new channels and spam
                    UI.PrintMessage("\nCreating new channels and spamming...");
                    for (int i = 0; i < channelCount; i++)
                    {
                        string channelName = $"{channelNameBase}-{i + 1}";
                        var content = new StringContent($"{{\"name\":\"{channelName}\",\"type\":0}}", Encoding.UTF8, "application/json");
                        var createResponse = await client.PostAsync($"https://discord.com/api/v10/guilds/{guildId}/channels", content);
                        if (createResponse.IsSuccessStatusCode)
                        {
                            JObject newChannel = JObject.Parse(await createResponse.Content.ReadAsStringAsync());
                            UI.PrintSuccess($"Created channel: {channelName}");
                            string newChannelId = newChannel["id"].ToString();

                            // Spam
                            var spamContent = new StringContent($"{{\"content\":\"{message}\"}}", Encoding.UTF8, "application/json");
                            for (int j = 0; j < msgCount; j++)
                            {
                                await client.PostAsync($"https://discord.com/api/v10/channels/{newChannelId}/messages", spamContent);
                                UI.PrintMessage($"  [{channelName}] Sent message {j + 1}/{msgCount}");
                                await Task.Delay(500);
                            }
                        }
                        else
                        {
                            UI.PrintError($"Failed to create channel {channelName} - {createResponse.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UI.PrintError($"Error during nuke: {ex.Message}");
                }
            }
        }

        static async Task WebhookInfo()
        {
            UI.PrintHeader("Webhook Information");
            string webhookUrl = UI.GetInput("Enter Webhook URL");
            if (string.IsNullOrWhiteSpace(webhookUrl)) { UI.PrintError("Webhook URL cannot be empty."); return; }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(webhookUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        JObject webhookData = JObject.Parse(await response.Content.ReadAsStringAsync());
                        UI.PrintInfo("Name", webhookData["name"]);
                        UI.PrintInfo("Webhook ID", webhookData["id"]);
                        UI.PrintInfo("Channel ID", webhookData["channel_id"]);
                        UI.PrintInfo("Guild ID", webhookData["guild_id"] ?? "N/A");
                        UI.PrintInfo("Token", webhookData["token"]);
                    }
                    else
                    {
                        UI.PrintError($"Failed to retrieve info. Status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    UI.PrintError($"Error: {ex.Message}");
                }
            }
        }

        static void NitroGiftGen()
        {
            UI.PrintHeader("Nitro Gift Generator");
            if (!int.TryParse(UI.GetInput("Enter number of codes to generate"), out int count) || count <= 0)
            {
                UI.PrintError("Invalid number.");
                return;
            }
            const string baseUrl = "https://discord.gift/";
            const int codeLength = 16;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            UI.PrintMessage("\nGenerated Codes:");
            for (int i = 0; i < count; i++)
            {
                string code = new string(Enumerable.Repeat(chars, codeLength)
                                                .Select(s => s[random.Next(s.Length)]).ToArray());
                UI.PrintMessage($"{baseUrl}{code}");
            }
        }

        static async Task WebhookMessage()
        {
            UI.PrintHeader("Send Webhook Message");
            string webhook = UI.GetInput("Webhook URL");
            if (string.IsNullOrWhiteSpace(webhook)) { UI.PrintError("URL cannot be empty."); return; }
            string message = UI.GetInput("Message");
            if (string.IsNullOrEmpty(message)) { UI.PrintError("Message cannot be empty."); return; }

            using (var client = new HttpClient())
            {
                var content = new StringContent($"{{\"content\":\"{message}\"}}", Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = await client.PostAsync(webhook, content);
                    if (response.IsSuccessStatusCode) UI.PrintSuccess("Message sent successfully.");
                    else UI.PrintError($"Failed to send message. HTTP Status: {response.StatusCode}");
                }
                catch (Exception ex) { UI.PrintError($"Error: {ex.Message}"); }
            }
        }

        static async Task WebhookSpammer()
        {
            UI.PrintHeader("Webhook Spammer");
            string webhook = UI.GetInput("Webhook URL");
            if (string.IsNullOrWhiteSpace(webhook)) { UI.PrintError("URL cannot be empty."); return; }
            string message = UI.GetInput("Message");
            if (string.IsNullOrEmpty(message)) { UI.PrintError("Message cannot be empty."); return; }
            if (!double.TryParse(UI.GetInput("Interval in seconds (e.g., 0.1)"), out double interval) || interval <= 0) { UI.PrintError("Invalid interval."); return; }

            UI.PrintWarning("\nSpamming... Press Enter to stop.");
            Task stopTask = Task.Run(Console.ReadLine);
            using (var client = new HttpClient())
            {
                var content = new StringContent($"{{\"content\":\"{message}\"}}", Encoding.UTF8, "application/json");
                while (!stopTask.IsCompleted)
                {
                    try { await client.PostAsync(webhook, content); }
                    catch { /* Ignore errors during spam */ }
                    await Task.Delay(TimeSpan.FromSeconds(interval));
                }
            }
            UI.PrintSuccess("Spamming stopped.");
        }

        static async Task WebhookDeleter()
        {
            UI.PrintHeader("Webhook Deleter");
            string webhook = UI.GetInput("Webhook URL");
            if (string.IsNullOrWhiteSpace(webhook)) { UI.PrintError("URL cannot be empty."); return; }

            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.DeleteAsync(webhook);
                    if (response.IsSuccessStatusCode) UI.PrintSuccess("Webhook deleted successfully.");
                    else UI.PrintError($"Failed to delete webhook. HTTP Status: {response.StatusCode}");
                }
                catch (Exception ex) { UI.PrintError($"Error: {ex.Message}"); }
            }
        }

        static async Task IpGeolocator()
        {
            UI.PrintHeader("IP Geolocator");
            string ipAddress = UI.GetInput("Enter IP Address");
            if (string.IsNullOrWhiteSpace(ipAddress)) { UI.PrintError("IP Address cannot be empty."); return; }

            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"http://ip-api.com/json/{ipAddress}");
                    if (response.IsSuccessStatusCode)
                    {
                        JObject geoData = JObject.Parse(await response.Content.ReadAsStringAsync());
                        UI.PrintInfo("IP", geoData["query"]);
                        UI.PrintInfo("City", geoData["city"]);
                        UI.PrintInfo("Region", geoData["regionName"]);
                        UI.PrintInfo("Country", geoData["country"]);
                        UI.PrintInfo("Coordinates", $"{geoData["lat"]}, {geoData["lon"]}");
                        UI.PrintInfo("ISP", geoData["isp"]);
                    }
                    else { UI.PrintError($"Failed to get geolocation data. HTTP Status: {response.StatusCode}"); }
                }
                catch (Exception ex) { UI.PrintError($"Error: {ex.Message}"); }
            }
        }

        static async Task IpPinger()
        {
            UI.PrintHeader("IP Pinger");
            string ipAddress = UI.GetInput("Enter IP Address to ping");
            if (string.IsNullOrWhiteSpace(ipAddress)) { UI.PrintError("IP Address cannot be empty."); return; }
            if (!int.TryParse(UI.GetInput("Number of times to ping"), out int count) || count <= 0) { UI.PrintError("Invalid number."); return; }

            UI.PrintMessage($"\nPinging {ipAddress}...");
            using (var pinger = new Ping())
            {
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        PingReply reply = await pinger.SendPingAsync(ipAddress);
                        if (reply.Status == IPStatus.Success)
                            UI.PrintSuccess($"  Ping {i + 1}: Reply from {reply.Address} in {reply.RoundtripTime}ms");
                        else
                            UI.PrintError($"  Ping {i + 1}: Failed - {reply.Status}");
                    }
                    catch (Exception ex) { UI.PrintError($"  Ping {i + 1}: Error - {ex.Message}"); }
                    await Task.Delay(1000);
                }
            }
        }

        static void ShowQRCodeGeneratorLink()
        {
            UI.PrintHeader("QR Code Generator");
            string link = "https://www.qr-code-generator.com";
            UI.PrintInfo("Link", link);
            UI.PrintMessage("\nThis free online tool can be used to create QR codes.");
        }

        static void Base64EncoderDecoder()
        {
            UI.PrintHeader("Base64 Encoder/Decoder");
            string choice = UI.GetInput("Enter 1 to Encode or 2 to Decode");
            if (choice == "1")
            {
                string input = UI.GetInput("Enter text to encode");
                if (string.IsNullOrEmpty(input)) { UI.PrintError("Input cannot be empty."); return; }
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
                UI.PrintInfo("Encoded", encoded);
            }
            else if (choice == "2")
            {
                string input = UI.GetInput("Enter Base64 to decode");
                if (string.IsNullOrEmpty(input)) { UI.PrintError("Input cannot be empty."); return; }
                try
                {
                    string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(input));
                    UI.PrintInfo("Decoded", decoded);
                }
                catch (FormatException) { UI.PrintError("Invalid Base64 string."); }
            }
            else { UI.PrintError("Invalid choice."); }
        }

        static void TimezoneConverter()
        {
            UI.PrintHeader("Timezone Converter");
            string dateTimeStr = UI.GetInput("Enter datetime (yyyy-MM-dd HH:mm:ss)");
            if (string.IsNullOrWhiteSpace(dateTimeStr)) { UI.PrintError("Datetime cannot be empty."); return; }
            string sourceOffsetStr = UI.GetInput("Enter source timezone offset (e.g., -5)");
            if (string.IsNullOrWhiteSpace(sourceOffsetStr)) { UI.PrintError("Offset cannot be empty."); return; }
            string targetOffsetStr = UI.GetInput("Enter target timezone offset (e.g., 2)");
            if (string.IsNullOrWhiteSpace(targetOffsetStr)) { UI.PrintError("Offset cannot be empty."); return; }

            try
            {
                DateTime dateTime = DateTime.ParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                double sourceOffset = double.Parse(sourceOffsetStr, CultureInfo.InvariantCulture);
                double targetOffset = double.Parse(targetOffsetStr, CultureInfo.InvariantCulture);
                DateTime utcTime = dateTime.AddHours(-sourceOffset);
                DateTime targetTime = utcTime.AddHours(targetOffset);
                UI.PrintInfo("Converted Time", $"{targetTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex) { UI.PrintError($"Error: {ex.Message}"); }
        }
    }
}
