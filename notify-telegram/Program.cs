using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace notify_telegram
{
    class Program
    {
        private static Config config;
        private static Object _configlock = new Object();
        private static DateTime lastMessage;
        private static ITelegramBotClient botClient;
        private static string configpath = Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "notify-telegram-config.json" });
        static void Main(string[] args)
        {
            try
            {
                if (File.Exists(configpath))
                {
                    config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configpath));
                }
                else
                {
                    config = new Config();
                    File.WriteAllText(configpath, JsonConvert.SerializeObject(config));
                    Console.WriteLine("notify-telegram is not configured! Please add Bot Token to config file at " + configpath);
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when accessing config file!" + Environment.NewLine + ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
                Environment.Exit(1);
            }
            

            if (args.Length < 1)
            {
                Console.WriteLine("Not enough arguments!");
                Console.WriteLine("Usage:  notify-telegram <message>");
                Environment.Exit(1);
            }


            string message = args[0];
            //string title = args[1];

            try
            {
                botClient = new TelegramBotClient(config.ApiToken);
                var me = botClient.GetMeAsync().Result;
                Console.WriteLine(
                  $"Telegram Bot {me.FirstName} with ID {me.Id} started."
                );
                botClient.OnMessage += Bot_OnMessage;
                lastMessage = DateTime.Now;
                botClient.StartReceiving();

                do
                {
                    Thread.Sleep(1000);
                } while ((DateTime.Now - lastMessage).TotalSeconds < 10);

                botClient.StopReceiving();

                Console.WriteLine($"Sending Notification to { config.ChatMembers.Count.ToString() } clients");

                foreach (long user in config.ChatMembers)
                {
                    _ = botClient.SendTextMessageAsync(user, message).Result;
                    Console.WriteLine("Sent to " + user.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while using Telegram Bot!" + Environment.NewLine + ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
                Environment.Exit(1);
            }
        }

        private static void SaveConfig()
        {
            lock(_configlock)
                File.WriteAllText(configpath, JsonConvert.SerializeObject(config));
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            lastMessage = DateTime.Now;
            string response = "";
            try
            {
                if (e.Message.Text != null)
                {
                    Console.WriteLine($"Received message from { e.Message.Chat.Id.ToString() }: { e.Message.Text }");
                    lock (_configlock)
                    {
                        switch (e.Message.Text)
                        {
                            case "/start":
                                if (!config.ChatMembers.Contains(e.Message.Chat.Id))
                                {
                                    config.ChatMembers.Add(e.Message.Chat.Id);
                                    response = "Welcome!";
                                }
                                break;
                            case "/stop":
                                if (config.ChatMembers.Contains(e.Message.Chat.Id))
                                {
                                    config.ChatMembers.Remove(e.Message.Chat.Id);
                                    response = "Bye!";
                                }
                                break;
                            default:
                                response = "Unknown command or message!";
                                break;
                        }
                        SaveConfig();
                    }
                }
                if (response != "")
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while using Telegram Bot!" + Environment.NewLine + ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
                botClient.StopReceiving();
                Environment.Exit(1);
            }
        }
    }
}
