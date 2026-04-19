using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Xunit;
using SteamSwitcherCore;

namespace SteamSwitcherTests
{
    public class IntegrationTests : IDisposable
    {
        private string _testConfigPath;

        public IntegrationTests()
        {
            // Setup before each test
            _testConfigPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
        }

        public void Dispose()
        {
            // Cleanup after each test
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
        }

        [Fact]
        public void ConfigPersistence_LoadAndSaveConfig()
        {
            // Arrange
            var originalConfig = new Config
            {
                SteamPath = @"C:\Program Files (x86)\Steam\steam.exe",
                Accounts = new Dictionary<string, string>
                {
                    ["Main Account"] = "mainuser",
                    ["Alt Account"] = "altuser"
                }
            };

            // Act: Save config
            string json = JsonSerializer.Serialize(originalConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testConfigPath, json);

            // Act: Load config via SteamAccountManager
            var manager = new SteamAccountManager(_testConfigPath);

            // Assert
            Assert.Equal(originalConfig.SteamPath, manager.GetSteamPath());
            Assert.Equal(originalConfig.Accounts.Count, manager.Accounts.Count);
            foreach (var account in originalConfig.Accounts)
            {
                Assert.True(manager.Accounts.ContainsKey(account.Key));
                Assert.Equal(account.Value, manager.Accounts[account.Key]);
            }
        }

        [Fact]
        public void ConfigFallback_UsesDefaultWhenCorrupted()
        {
            // Arrange: Create corrupted config
            File.WriteAllText(_testConfigPath, "{ invalid json }");

            // Act
            var manager = new SteamAccountManager(_testConfigPath);

            // Assert: Should fallback to default
            Assert.Equal("Need to add", manager.GetSteamPath());
            Assert.True(manager.Accounts.Count >= 1); // At least default accounts
        }

        [Fact]
        public void AccountSwitching_ValidatesAccountExistence()
        {
            // Arrange
            var testConfig = new Config
            {
                SteamPath = @"C:\Fake\Steam.exe", // Fake path to avoid actual process start
                Accounts = new Dictionary<string, string>
                {
                    ["ValidAccount"] = "validuser"
                }
            };
            string json = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testConfigPath, json);
            var manager = new SteamAccountManager(_testConfigPath);

            // Act & Assert: Valid account should attempt to switch (but fail due to fake path)
            bool result = manager.TrySwitchAccount("ValidAccount", out string message);
            Assert.False(result); // Should fail because Steam.exe doesn't exist
            Assert.Contains("не найден", message);
        }

        [Fact]
        public void Logging_IntegrationWithLogAction()
        {
            // Arrange
            var logMessages = new List<string>();
            var testConfig = new Config
            {
                SteamPath = @"C:\Fake\Steam.exe",
                Accounts = new Dictionary<string, string>
                {
                    ["TestAccount"] = "testuser"
                }
            };
            string json = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testConfigPath, json);
            var manager = new SteamAccountManager(_testConfigPath);

            // Act
            manager.TrySwitchAccount("TestAccount", msg => logMessages.Add(msg), out _);

            // Assert: Should have logged messages
            Assert.True(logMessages.Count > 0);
            Assert.Contains(logMessages, msg => msg.Contains("Закрываем Steam") || msg.Contains("не найден"));
        }

        private sealed class Config
        {
            public string SteamPath { get; set; } = "";
            public Dictionary<string, string>? Accounts { get; set; } = new();
        }
    }
}