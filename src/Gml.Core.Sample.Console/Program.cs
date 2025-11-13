using System.Net;
using Gml.Core.Launcher;
using Gml.Domains.Launcher;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;

namespace Gml.Core.Sample.Console
{
    internal class Program
    {
        private const string LauncherName = "GmlServer";
        private const string SecurityKey = "gfweagertghuysergfbsuyerbgiuyserg";
        private const string TestUserName = "testUser";
        private const string TestUserPassword = "testPassword";

        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Gml.Core Sample Console Application");
            System.Console.WriteLine("====================================");

            // Инициализация GmlManager
            var gmlManager = new GmlManager(new GmlSettings(LauncherName, SecurityKey, httpClient: new HttpClient())
            {
                TextureServiceEndpoint = "http://localhost:8085" // Замените на ваш эндпоинт, если необходимо
            });

            // Восстановление настроек (при необходимости)
            gmlManager.RestoreSettings<LauncherVersion>();

            await ShowMenu(gmlManager);
        }

        private static async Task ShowMenu(IGmlManager gmlManager)
        {
            bool exit = false;

            while (!exit)
            {
                System.Console.WriteLine("\nВыберите операцию:");
                System.Console.WriteLine("1. Управление профилями");
                System.Console.WriteLine("2. Управление пользователями");
                System.Console.WriteLine("3. Управление серверами");
                System.Console.WriteLine("4. Управление модами");
                System.Console.WriteLine("0. Выход");

                System.Console.Write("\nВаш выбор: ");
                string? input = System.Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await ManageProfiles(gmlManager);
                        break;
                    case "2":
                        await ManageUsers(gmlManager);
                        break;
                    case "3":
                        await ManageServers(gmlManager);
                        break;
                    case "4":
                        await ManageMods(gmlManager);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        System.Console.WriteLine("Неверный выбор. Пожалуйста, повторите.");
                        break;
                }
            }
        }

        private static async Task ManageProfiles(IGmlManager gmlManager)
        {
            System.Console.WriteLine("\n--- Управление профилями ---");
            System.Console.WriteLine("1. Показать список профилей");
            System.Console.WriteLine("2. Создать новый профиль");
            System.Console.WriteLine("3. Удалить профиль");
            System.Console.WriteLine("4. Список доступных версий для лоадера");
            System.Console.WriteLine("0. Назад");

            System.Console.Write("\nВаш выбор: ");
            string? input = System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    await ShowProfiles(gmlManager);
                    break;
                case "2":
                    await CreateProfile(gmlManager);
                    break;
                case "3":
                    await DeleteProfile(gmlManager);
                    break;
                case "4":
                    await ShowVersionsForLoader(gmlManager);
                    break;
                case "0":
                    // Возврат в главное меню
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор. Пожалуйста, повторите.");
                    break;
            }
        }

        private static async Task ShowProfiles(IGmlManager gmlManager)
        {
            var profiles = await gmlManager.Profiles.GetProfiles();

            if (profiles.Count == 0)
            {
                System.Console.WriteLine("Нет доступных профилей.");
                return;
            }

            System.Console.WriteLine("\nСписок профилей:");
            int index = 1;

            foreach (var profile in profiles)
            {
                System.Console.WriteLine($"{index++}. {profile.Name} - {profile.GameVersion} ({profile.Loader})");
            }
        }

        private static async Task CreateProfile(IGmlManager gmlManager)
        {
            System.Console.Write("Введите имя профиля: ");
            string? profileName = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(profileName))
            {
                System.Console.WriteLine("Имя профиля не может быть пустым.");
                return;
            }

            System.Console.Write("Введите версию Minecraft (например, 1.20.1): ");
            string? gameVersion = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(gameVersion))
            {
                System.Console.WriteLine("Версия Minecraft не может быть пустой.");
                return;
            }

            System.Console.WriteLine("Выберите лоадер:");
            System.Console.WriteLine("1. Vanilla");
            System.Console.WriteLine("2. Forge");
            System.Console.WriteLine("3. Fabric");
            System.Console.WriteLine("4. LiteLoader");
            System.Console.WriteLine("5. NeoForge");
            System.Console.WriteLine("6. Quilt");

            System.Console.Write("Ваш выбор: ");
            string? loaderChoice = System.Console.ReadLine();

            GameLoader loader;
            switch (loaderChoice)
            {
                case "1":
                    loader = GameLoader.Vanilla;
                    break;
                case "2":
                    loader = GameLoader.Forge;
                    break;
                case "3":
                    loader = GameLoader.Fabric;
                    break;
                case "4":
                    loader = GameLoader.LiteLoader;
                    break;
                case "5":
                    loader = GameLoader.NeoForge;
                    break;
                case "6":
                    loader = GameLoader.Quilt;
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор лоадера.");
                    return;
            }

            string loaderVersion = string.Empty;

            if (loader != GameLoader.Vanilla)
            {
                var versions = await gmlManager.Profiles.GetAllowVersions(loader, gameVersion);

                if (!versions.Any())
                {
                    System.Console.WriteLine($"Нет доступных версий для {loader} с версией Minecraft {gameVersion}");
                    return;
                }

                System.Console.WriteLine("Доступные версии лоадера:");
                int index = 1;

                foreach (var version in versions)
                {
                    System.Console.WriteLine($"{index++}. {version}");
                }

                System.Console.Write("Выберите номер версии: ");
                string? versionChoice = System.Console.ReadLine();

                if (int.TryParse(versionChoice, out int versionIndex) && versionIndex > 0 && versionIndex <= versions.Count())
                {
                    loaderVersion = versions.ElementAt(versionIndex - 1);
                }
                else
                {
                    System.Console.WriteLine("Неверный выбор версии.");
                    return;
                }
            }

            try
            {
                var profile = await gmlManager.Profiles.AddProfile(
                    profileName,
                    profileName,
                    gameVersion,
                    loaderVersion,
                    loader,
                    string.Empty,
                    string.Empty);

                if (profile != null)
                {
                    System.Console.WriteLine($"Профиль {profileName} успешно создан!");
                }
                else
                {
                    System.Console.WriteLine("Не удалось создать профиль.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ошибка при создании профиля: {ex.Message}");
            }
        }

        private static async Task DeleteProfile(IGmlManager gmlManager)
        {
            var profiles = await gmlManager.Profiles.GetProfiles();

            if (!profiles.Any())
            {
                System.Console.WriteLine("Нет доступных профилей для удаления.");
                return;
            }

            System.Console.WriteLine("\nВыберите профиль для удаления:");
            int index = 1;

            foreach (var profile in profiles)
            {
                System.Console.WriteLine($"{index++}. {profile.Name} - {profile.GameVersion} ({profile.Loader})");
            }

            System.Console.Write("Введите номер профиля для удаления: ");
            string? input = System.Console.ReadLine();

            if (int.TryParse(input, out int profileIndex) && profileIndex > 0 && profileIndex <= profiles.Count())
            {
                var profileToDelete = profiles.ElementAt(profileIndex - 1);

                System.Console.Write($"Вы уверены, что хотите удалить профиль {profileToDelete.Name}? (y/n): ");
                string? confirmation = System.Console.ReadLine();

                if (confirmation?.ToLower() == "y")
                {
                    await profileToDelete.Remove();
                    System.Console.WriteLine($"Профиль {profileToDelete.Name} успешно удален.");
                }
                else
                {
                    System.Console.WriteLine("Удаление профиля отменено.");
                }
            }
            else
            {
                System.Console.WriteLine("Неверный выбор профиля.");
            }
        }

        private static async Task ShowVersionsForLoader(IGmlManager gmlManager)
        {
            System.Console.WriteLine("Выберите лоадер:");
            System.Console.WriteLine("1. Vanilla");
            System.Console.WriteLine("2. Forge");
            System.Console.WriteLine("3. Fabric");
            System.Console.WriteLine("4. LiteLoader");
            System.Console.WriteLine("5. NeoForge");
            System.Console.WriteLine("6. Quilt");

            System.Console.Write("Ваш выбор: ");
            string? loaderChoice = System.Console.ReadLine();

            GameLoader loader;
            switch (loaderChoice)
            {
                case "1":
                    loader = GameLoader.Vanilla;
                    break;
                case "2":
                    loader = GameLoader.Forge;
                    break;
                case "3":
                    loader = GameLoader.Fabric;
                    break;
                case "4":
                    loader = GameLoader.LiteLoader;
                    break;
                case "5":
                    loader = GameLoader.NeoForge;
                    break;
                case "6":
                    loader = GameLoader.Quilt;
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор лоадера.");
                    return;
            }

            string? gameVersion = string.Empty;

            if (loader != GameLoader.Vanilla)
            {
                System.Console.Write("Введите версию Minecraft (например, 1.20.1): ");
                gameVersion = System.Console.ReadLine();

                if (string.IsNullOrWhiteSpace(gameVersion))
                {
                    System.Console.WriteLine("Версия Minecraft не может быть пустой.");
                    return;
                }
            }

            try
            {
                var versions = await gmlManager.Profiles.GetAllowVersions(loader, gameVersion);

                if (!versions.Any())
                {
                    System.Console.WriteLine($"Нет доступных версий для {loader}" +
                                        (string.IsNullOrWhiteSpace(gameVersion) ? "" : $" с версией Minecraft {gameVersion}"));
                    return;
                }

                System.Console.WriteLine("\nДоступные версии:");
                int index = 1;

                foreach (var version in versions)
                {
                    System.Console.WriteLine($"{index++}. {version}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ошибка при получении версий: {ex.Message}");
            }
        }

        private static async Task ManageUsers(IGmlManager gmlManager)
        {
            System.Console.WriteLine("\n--- Управление пользователями ---");
            System.Console.WriteLine("1. Показать список пользователей");
            System.Console.WriteLine("2. Аутентификация пользователя");
            System.Console.WriteLine("3. Найти пользователя по имени");
            System.Console.WriteLine("0. Назад");

            System.Console.Write("\nВаш выбор: ");
            string? input = System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    await ShowUsers(gmlManager);
                    break;
                case "2":
                    await AuthenticateUser(gmlManager);
                    break;
                case "3":
                    await FindUserByName(gmlManager);
                    break;
                case "0":
                    // Возврат в главное меню
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор. Пожалуйста, повторите.");
                    break;
            }
        }

        private static async Task ShowUsers(IGmlManager gmlManager)
        {
            var users = await gmlManager.Users.GetUsers();

            if (!users.Any())
            {
                System.Console.WriteLine("Нет доступных пользователей.");
                return;
            }

            System.Console.WriteLine("\nСписок пользователей:");
            int index = 1;

            foreach (var user in users)
            {
                System.Console.WriteLine($"{index++}. {user.Name} (UUID: {user.Uuid})");
            }
        }

        private static async Task AuthenticateUser(IGmlManager gmlManager)
        {
            // Получаем доступные сервисы аутентификации
            var services = await gmlManager.Integrations.GetAuthServices();

            if (services.Count == 0)
            {
                System.Console.WriteLine("Нет доступных сервисов аутентификации.");
                return;
            }

            System.Console.WriteLine("\nДоступные сервисы аутентификации:");
            int index = 1;

            foreach (var service in services)
            {
                System.Console.WriteLine($"{index++}. {service.Name} ({service.AuthType})");
            }

            System.Console.Write("Выберите сервис аутентификации: ");
            string? serviceChoice = System.Console.ReadLine();

            if (int.TryParse(serviceChoice, out int serviceIndex) && serviceIndex > 0 && serviceIndex <= services.Count())
            {
                var selectedService = services.ElementAt(serviceIndex - 1);

                // Устанавливаем выбранный сервис как активный
                await gmlManager.Integrations.SetActiveAuthService(selectedService);

                System.Console.Write("Введите имя пользователя: ");
                string? username = System.Console.ReadLine();

                System.Console.Write("Введите пароль: ");
                string? password = System.Console.ReadLine();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    System.Console.WriteLine("Имя пользователя и пароль не могут быть пустыми.");
                    return;
                }

                try
                {
                    var user = await gmlManager.Users.GetAuthData(
                        username,
                        password,
                        "Desktop",
                        "1.0",
                        IPAddress.Parse("127.0.0.1"),
                        null,
                        null,
                        false);

                    System.Console.WriteLine($"Аутентификация успешна! Добро пожаловать, {user.Name}!");
                    System.Console.WriteLine($"UUID: {user.Uuid}");
                    System.Console.WriteLine($"AccessToken: {user.AccessToken}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Ошибка при аутентификации: {ex.Message}");
                }
            }
            else
            {
                System.Console.WriteLine("Неверный выбор сервиса аутентификации.");
            }
        }

        private static async Task FindUserByName(IGmlManager gmlManager)
        {
            System.Console.Write("Введите имя пользователя для поиска: ");
            string? username = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(username))
            {
                System.Console.WriteLine("Имя пользователя не может быть пустым.");
                return;
            }

            try
            {
                var user = await gmlManager.Users.GetUserByName(username);

                if (user != null)
                {
                    System.Console.WriteLine($"Пользователь найден!");
                    System.Console.WriteLine($"Имя: {user.Name}");
                    System.Console.WriteLine($"UUID: {user.Uuid}");
                }
                else
                {
                    System.Console.WriteLine($"Пользователь с именем {username} не найден.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ошибка при поиске пользователя: {ex.Message}");
            }
        }

        private static async Task ManageServers(IGmlManager gmlManager)
        {
            System.Console.WriteLine("\n--- Управление серверами ---");
            System.Console.WriteLine("1. Показать список серверов для профиля");
            System.Console.WriteLine("2. Добавить сервер к профилю");
            System.Console.WriteLine("3. Удалить сервер из профиля");
            System.Console.WriteLine("0. Назад");

            System.Console.Write("\nВаш выбор: ");
            string? input = System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    await ShowServersForProfile(gmlManager);
                    break;
                case "2":
                    await AddServerToProfile(gmlManager);
                    break;
                case "3":
                    await RemoveServerFromProfile(gmlManager);
                    break;
                case "0":
                    // Возврат в главное меню
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор. Пожалуйста, повторите.");
                    break;
            }
        }

        private static async Task ShowServersForProfile(IGmlManager gmlManager)
        {
            var profile = await SelectProfile(gmlManager);

            if (profile == null)
                return;

            var servers = profile.Servers;

            if (!servers.Any())
            {
                System.Console.WriteLine($"Нет серверов для профиля {profile.Name}.");
                return;
            }

            System.Console.WriteLine($"\nСерверы для профиля {profile.Name}:");
            int index = 1;

            foreach (var server in servers)
            {
                System.Console.WriteLine($"{index++}. {server.Name} - {server.Address}:{server.Port}");
            }
        }

        private static async Task AddServerToProfile(IGmlManager gmlManager)
        {
            var profile = await SelectProfile(gmlManager);

            if (profile == null)
                return;

            System.Console.Write("Введите имя сервера: ");
            string? serverName = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(serverName))
            {
                System.Console.WriteLine("Имя сервера не может быть пустым.");
                return;
            }

            System.Console.Write("Введите адрес сервера: ");
            string? serverAddress = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                System.Console.WriteLine("Адрес сервера не может быть пустым.");
                return;
            }

            System.Console.Write("Введите порт сервера (по умолчанию 25565): ");
            string? portInput = System.Console.ReadLine();

            int port = 25565;
            if (!string.IsNullOrWhiteSpace(portInput) && !int.TryParse(portInput, out port))
            {
                System.Console.WriteLine("Неверный формат порта. Будет использовано значение по умолчанию (25565).");
                port = 25565;
            }

            try
            {
                var server = await gmlManager.Servers.AddMinecraftServer(profile, serverName, serverAddress, port);

                System.Console.WriteLine($"Сервер {server.Name} успешно добавлен к профилю {profile.Name}!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ошибка при добавлении сервера: {ex.Message}");
            }
        }

        private static async Task RemoveServerFromProfile(IGmlManager gmlManager)
        {
            var profile = await SelectProfile(gmlManager);

            if (profile == null)
                return;

            var servers = profile.Servers;

            if (!servers.Any())
            {
                System.Console.WriteLine($"Нет серверов для удаления в профиле {profile.Name}.");
                return;
            }

            System.Console.WriteLine($"\nВыберите сервер для удаления из профиля {profile.Name}:");
            int index = 1;

            foreach (var server in servers)
            {
                System.Console.WriteLine($"{index++}. {server.Name} - {server.Address}:{server.Port}");
            }

            System.Console.Write("Введите номер сервера для удаления: ");
            string? input = System.Console.ReadLine();

            if (int.TryParse(input, out int serverIndex) && serverIndex > 0 && serverIndex <= servers.Count())
            {
                var serverToDelete = servers.ElementAt(serverIndex - 1);

                System.Console.Write($"Вы уверены, что хотите удалить сервер {serverToDelete.Name}? (y/n): ");
                string? confirmation = System.Console.ReadLine();

                if (confirmation?.ToLower() == "y")
                {
                    await gmlManager.Servers.RemoveServer(profile, serverToDelete.Name);
                    System.Console.WriteLine($"Сервер {serverToDelete.Name} успешно удален из профиля {profile.Name}.");
                }
                else
                {
                    System.Console.WriteLine("Удаление сервера отменено.");
                }
            }
            else
            {
                System.Console.WriteLine("Неверный выбор сервера.");
            }
        }

        private static async Task ManageMods(IGmlManager gmlManager)
        {
            System.Console.WriteLine("\n--- Управление модами ---");
            System.Console.WriteLine("1. Найти моды");
            System.Console.WriteLine("2. Показать моды профиля");
            System.Console.WriteLine("0. Назад");

            System.Console.Write("\nВаш выбор: ");
            string? input = System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    await FindMods(gmlManager);
                    break;
                case "2":
                    await ShowModsForProfile(gmlManager);
                    break;
                case "0":
                    // Возврат в главное меню
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор. Пожалуйста, повторите.");
                    break;
            }
        }

        private static async Task FindMods(IGmlManager gmlManager)
        {
            var profile = await SelectProfile(gmlManager);

            if (profile == null)
                return;

            System.Console.WriteLine("Выберите тип модов для поиска:");
            System.Console.WriteLine("1. Modrinth");
            System.Console.WriteLine("2. CurseForge");

            System.Console.Write("Ваш выбор: ");
            string? modTypeChoice = System.Console.ReadLine();

            ModType modType;
            switch (modTypeChoice)
            {
                case "1":
                    modType = ModType.Modrinth;
                    break;
                case "2":
                    modType = ModType.CurseForge;
                    break;
                default:
                    System.Console.WriteLine("Неверный выбор типа модов.");
                    return;
            }

            System.Console.Write("Введите поисковый запрос (или оставьте пустым для поиска всех модов): ");
            string? searchQuery = System.Console.ReadLine();

            try
            {
                var mods = await gmlManager.Mods.FindModsAsync(
                    profile.Loader,
                    profile.GameVersion,
                    modType,
                    searchQuery ?? string.Empty,
                    10,
                    0);

                if (!mods.Any())
                {
                    System.Console.WriteLine("Не найдено модов, соответствующих запросу.");
                    return;
                }

                System.Console.WriteLine("\nНайденные моды:");
                int index = 1;

                foreach (var mod in mods)
                {
                    System.Console.WriteLine($"{index++}. {mod.Name} - {mod.Description.Substring(0, Math.Min(mod.Description.Length, 100))}...");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ошибка при поиске модов: {ex.Message}");
            }
        }

        private static async Task ShowModsForProfile(IGmlManager gmlManager)
        {
            var profile = await SelectProfile(gmlManager);

            if (profile == null)
                return;

            try
            {
                var mods = await profile.GetModsAsync();

                if (mods.Count == 0)
                {
                    System.Console.WriteLine($"В профиле {profile.Name} нет установленных модов.");
                    return;
                }

                System.Console.WriteLine($"\nМоды для профиля {profile.Name}:");
                int index = 1;

                foreach (var mod in mods)
                {
                    System.Console.WriteLine($"{index++}. {mod.Name} - {mod.Type}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ошибка при получении списка модов: {ex.Message}");
            }
        }

        // Вспомогательный метод для выбора профиля
        private static async Task<IGameProfile?> SelectProfile(IGmlManager gmlManager)
        {
            var profiles = await gmlManager.Profiles.GetProfiles();

            if (!profiles.Any())
            {
                System.Console.WriteLine("Нет доступных профилей.");
                return null;
            }

            System.Console.WriteLine("\nВыберите профиль:");
            int index = 1;

            foreach (var profile in profiles)
            {
                System.Console.WriteLine($"{index++}. {profile.Name} - {profile.GameVersion} ({profile.Loader})");
            }

            System.Console.Write("Ваш выбор: ");
            string? input = System.Console.ReadLine();

            if (int.TryParse(input, out int profileIndex) && profileIndex > 0 && profileIndex <= profiles.Count())
            {
                return profiles.ElementAt(profileIndex - 1);
            }

            System.Console.WriteLine("Неверный выбор профиля.");
            return null;
        }
    }
}
