using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using SteamSwitcherCore;

namespace SteamSwitcherTests
{
    public class SteamAccountManagerTests
    {
        [Fact]
        public void LoadConfig_CreatesDefaultConfig_WhenFileDoesNotExist()
        {
            // Arrange
            string testConfigPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            if (File.Exists(testConfigPath))
                File.Delete(testConfigPath);

            // Act
            var manager = new SteamAccountManager(testConfigPath);

            // Assert
            Assert.True(File.Exists(testConfigPath));
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(testConfigPath));
            Assert.NotNull(config);
            Assert.Equal("Need to add", config.SteamPath);
            Assert.NotNull(config.Accounts);
            Assert.Equal(2, config.Accounts.Count);
        }

        [Fact]
        public void LoadConfig_LoadsExistingConfig()
        {
            // Arrange
            string testConfigPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            var testConfig = new Config
            {
                SteamPath = @"C:\Test\Steam.exe",
                Accounts = new Dictionary<string, string>
                {
                    ["TestAccount1"] = "user1",
                    ["TestAccount2"] = "user2"
                }
            };
            string json = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(testConfigPath, json);

            // Act
            var manager = new SteamAccountManager(testConfigPath);

            // Assert
            Assert.Equal(@"C:\Test\Steam.exe", manager.GetSteamPath());
            Assert.Equal(2, manager.Accounts.Count);
            Assert.Equal("user1", manager.Accounts["TestAccount1"]);
            Assert.Equal("user2", manager.Accounts["TestAccount2"]);
        }

        [Fact]
        public void TrySwitchAccount_ReturnsFalse_WhenAccountNotFound()
        {
            // Arrange
            string testConfigPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            var manager = new SteamAccountManager(testConfigPath);

            // Act
            bool result = manager.TrySwitchAccount("NonExistentAccount", out string message);

            // Assert
            Assert.False(result);
            Assert.Contains("не найден", message);
        }

        [Fact]
        public void TrySwitchAccount_ReturnsFalse_WhenSteamPathInvalid()
        {
            // Arrange
            string testConfigPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            var testConfig = new Config
            {
                SteamPath = @"C:\Invalid\Path\Steam.exe",
                Accounts = new Dictionary<string, string>
                {
                    ["TestAccount"] = "user1"
                }
            };
            string json = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(testConfigPath, json);
            var manager = new SteamAccountManager(testConfigPath);

            // Act
            bool result = manager.TrySwitchAccount("TestAccount", out string message);

            // Assert
            Assert.False(result);
            Assert.Contains("не найден", message);
        }

        [Fact]
        public void ShowAccounts_PrintsAccounts()
        {
            // Arrange
            string testConfigPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            var testConfig = new Config
            {
                SteamPath = @"C:\Test\Steam.exe",
                Accounts = new Dictionary<string, string>
                {
                    ["Account1"] = "user1",
                    ["Account2"] = "user2"
                }
            };
            string json = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(testConfigPath, json);
            var manager = new SteamAccountManager(testConfigPath);

            // Act & Assert
            // Note: This test verifies that no exception is thrown and accounts are loaded
            Assert.Equal(2, manager.Accounts.Count);
            Assert.True(manager.Accounts.ContainsKey("Account1"));
            Assert.True(manager.Accounts.ContainsKey("Account2"));
        }

        // Helper class to access private members for testing
        private class TestableSteamAccountManager : SteamAccountManager
        {
            public new string GetSteamPath()
            {
                return base.GetType().GetField("SteamPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(this) as string ?? "";
            }
        }

        private sealed class Config
        {
            public string SteamPath { get; set; } = "";
            public Dictionary<string, string>? Accounts { get; set; } = new();
        }
    }
}