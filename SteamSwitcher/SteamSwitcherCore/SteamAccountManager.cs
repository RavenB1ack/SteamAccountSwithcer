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

        public SteamAccountManager()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            var config = ReadConfig(configPath);

            SteamPath = string.IsNullOrWhiteSpace(config.SteamPath) ? DefaultSteamPath : config.SteamPath;
            Accounts.Clear();

            if (config.Accounts != null)
            {
                foreach (var account in config.Accounts)
                    Accounts[account.Key] = account.Value;
            }
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
                    ["Need to Add"] = "need_to_add", //You can add default accounts
                    ["Need to Add"] = "need_to_add"
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

        private sealed class Config
        {
            public string SteamPath { get; set; } = DefaultSteamPath;
            public Dictionary<string, string>? Accounts { get; set; } = new();
        }

        public void ShowAccounts()
        {
            Console.WriteLine("\n=== Доступные аккаунты Steam ===");
            int index = 1;
            foreach (var account in Accounts)
            {
                Console.WriteLine($"{index}. {account.Key} ({account.Value})");
                index++;
            }
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
