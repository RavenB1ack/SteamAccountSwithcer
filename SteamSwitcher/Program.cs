using SteamSwitcherCore;
using System;
using System.Collections.Generic;

namespace SteamAccountSwitcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new SteamAccountManager();

            // 🔥 Запуск с аргументом
            if (args.Length > 0)
            {
                if (manager.TrySwitchAccount(args[0], out string message))
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.WriteLine("❌ " + message);
                }

                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
                return;
            }

            bool running = true;

            while (running)
            {
                Console.Clear();

                Console.WriteLine("╔════════════════════════════════╗");
                Console.WriteLine("║   Steam Account Switcher       ║");
                Console.WriteLine("╚════════════════════════════════╝");
                Console.WriteLine("1. Переключиться");
                Console.WriteLine("2. Показать аккаунты");
                Console.WriteLine("3. Добавить аккаунт");
                Console.WriteLine("4. Выход");
                Console.Write("\nВыбор: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        manager.ShowAccounts();
                        Console.Write("\nВведите номер: ");

                        if (int.TryParse(Console.ReadLine(), out int accountNum))
                        {
                            var list = new List<string>(manager.Accounts.Keys);

                            if (accountNum > 0 && accountNum <= list.Count)
                                manager.TrySwitchAccount(list[accountNum - 1], out _);
                        }

                        Pause();
                        break;

                    case "2":
                        manager.ShowAccounts();
                        Pause();
                        break;

                    case "3":
                        Console.Write("Название: ");
                        string name = Console.ReadLine() ?? "";

                        Console.Write("Username: ");
                        string username = Console.ReadLine() ?? "";

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(username))
                            manager.AddAccount(name, username);

                        Pause();
                        break;

                    case "4":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("❌ Неверный выбор");
                        Pause();
                        break;
                }
            }
        }

        static void Pause()
        {
            Console.WriteLine("\nНажмите Enter...");
            Console.ReadLine();
        }
    }
}