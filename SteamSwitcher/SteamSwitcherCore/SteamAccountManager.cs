using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace SteamSwitcherCore
{
    public class SteamAccountManager
    {
        private const string DefaultSteamPath = @"Need to add"; //You can add default path
        private const string ConfigFileName = "steamconfig.json";

        private string SteamPath = DefaultSteamPath;
        public Dictionary<string, string> Accounts { get; } = new();
        public bool IsWidgetMode { get; private set; }
        public int WidgetPositionX { get; private set; }
        public int WidgetPositionY { get; private set; }

        public SteamAccountManager()
        {
            LoadConfig();
        }

        public SteamAccountManager(string configPath)
        {
            LoadConfig(configPath);
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            LoadConfig(configPath);
        }

        private void LoadConfig(string configPath)
        {
            var config = ReadConfig(configPath);

            SteamPath = string.IsNullOrWhiteSpace(config.SteamPath) ? DefaultSteamPath : config.SteamPath;
            Accounts.Clear();

            if (config.Accounts != null)
            {
                foreach (var account in config.Accounts)
                    Accounts[account.Key] = account.Value;
            }

            IsWidgetMode = config.IsWidgetMode;
            WidgetPositionX = config.WidgetPositionX;
            WidgetPositionY = config.WidgetPositionY;
        }

        private Config ReadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var defaultConfig = CreateDefaultConfig();
                WriteConfig(filePath, defaultConfig);
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                    throw new InvalidOperationException("Конфиг не может быть пустым.");

                if (string.IsNullOrWhiteSpace(config.SteamPath))
                    config.SteamPath = DefaultSteamPath;

                if (config.Accounts == null)
                    config.Accounts = new Dictionary<string, string>();

                return config;
            }
            catch
            {
                var fallbackConfig = CreateDefaultConfig();
                WriteConfig(filePath, fallbackConfig);
                return fallbackConfig;
            }
        }

        private Config CreateDefaultConfig()
        {
            return new Config
            {
                SteamPath = DefaultSteamPath,
                Accounts = new Dictionary<string, string>
                {
                    ["Account 1"] = "need_to_add", //You can add default accounts
                    ["Account 2"] = "need_to_add"
                }
            };
        }

        private void WriteConfig(string filePath, Config config)
        {
            string directory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void SaveWidgetSettings(bool isWidgetMode, int x, int y)
        {
            IsWidgetMode = isWidgetMode;
            WidgetPositionX = x;
            WidgetPositionY = y;

            string configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            var config = ReadConfig(configPath);
            config.IsWidgetMode = isWidgetMode;
            config.WidgetPositionX = x;
            config.WidgetPositionY = y;
            WriteConfig(configPath, config);
        }

        private sealed class Config
        {
            public string SteamPath { get; set; } = DefaultSteamPath;
            public Dictionary<string, string>? Accounts { get; set; } = new();
            public bool IsWidgetMode { get; set; } = false;
            public int WidgetPositionX { get; set; } = 100;
            public int WidgetPositionY { get; set; } = 100;
        }

        public string GetSteamPath()
        {
            return SteamPath;
        }

        public bool TrySwitchAccount(string accountKey, out string resultMessage)
        {
            return TrySwitchAccount(accountKey, msg => Console.WriteLine(msg), out resultMessage);
        }

        public bool TrySwitchAccount(string accountKey, Action<string> logAction, out string resultMessage)
        {
            accountKey = accountKey.Trim();

            var match = Accounts.Keys
                .FirstOrDefault(k => k.Equals(accountKey, StringComparison.OrdinalIgnoreCase));

            void Log(string message)
            {
                try
                {
                    logAction(message);
                }
                catch
                {
                }
            }

            if (match == null)
            {
                resultMessage = $"Аккаунт '{accountKey}' не найден.";
                return false;
            }

            if (!File.Exists(SteamPath))
            {
                Log($"❌ Steam.exe не найден: {SteamPath}");
                resultMessage = $"Steam.exe не найден: {SteamPath}";
                return false;
            }

            string username = Accounts[match];
            string args = $"-login {username}";

            try
            {
                Log("⛔ Закрываем Steam...");
                KillSteamProcess();
                Log("⏳ Ждем завершения процесса Steam...");
                Thread.Sleep(4000);

                Log($"🔄 Запуск Steam для {match} ({username})...");

                ProcessStartInfo startInfo = new()
                {
                    FileName = SteamPath,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);

                resultMessage = $"Steam запущен для {match} ({username}).";
                return true;
            }
            catch (Exception ex)
            {
                resultMessage = $"Ошибка при запуске Steam: {ex.Message}";
                return false;
            }
        }

        private void KillSteamProcess()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("steam"))
                {
                    process.Kill();
                    process.WaitForExit();
                }

                foreach (var process in Process.GetProcessesByName("steamwebhelper"))
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка при закрытии Steam: {ex.Message}");
            }
        }

        public void AddAccount(string name, string username)
        {
            Accounts[name] = username;
            Console.WriteLine($"✅ Добавлен: {name}");
        }
    }
}
