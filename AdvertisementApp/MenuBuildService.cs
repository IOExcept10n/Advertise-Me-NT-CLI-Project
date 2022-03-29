using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdvertisementApp
{
    public static class MenuBuildService
    {
        //Набор регулярных выражений для проверки различных вводимых данных. URL так и не стал проверять, но может потом буду.

        static readonly Regex emailRegex = new Regex(@"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$", RegexOptions.Compiled);

        static readonly Regex urlRegex = new Regex(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        
        static readonly Regex phoneRegex = new Regex(@"^(\s*)?(\+)?([- _():=+]?\d[- _():=+]?){10,14}(\s*)?$", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        /// <summary>
        /// Настройка всех меню в соответствии с текстом и действий по нажатию.
        /// </summary>
        /// <returns></returns>
        public static ConsoleMenu BuildMenuSystem()
        {
            ConsoleMenu accountSettings = new ConsoleMenu("Выберите опцию: ")
            {
                Elements = new List<MenuElement>()
                {
                    new MenuElement("Сменить никнейм", onclick:ChangeName),
                    new MenuElement("Сменить пароль", onclick:ChangePassword),
                    new MenuElement("Вернуться в главное меню", onclick:() => Task.Run(() => Program.ResetMenu()))
                }
            };
            ConsoleMenu adminMenu = new ConsoleMenu("Выберите действие: ")
            {
                Elements = new List<MenuElement>()
                {
                    new MenuElement("Управление пользователями", onclick:ChangeName),
                    new MenuElement("Выполнить собственный SQL-запрос", onclick:ChangePassword),
                    new MenuElement("Вернуться в главное меню", onclick:() => Task.Run(() => Program.ResetMenu()))
                }
            };
            ConsoleMenu menu = new ConsoleMenu("Выберите действие: ")
            {
                Elements = new List<MenuElement>()
                {
                    new MenuElement("Открыть список рекламных объявлений", onclick:ShowAdvertisementsAsync),
                    new MenuElement("Открыть список рекламодателей", onclick:ShowAdvertisersAsync),
                    new MenuElement("Найти объявление по названию", ConsoleColor.DarkCyan, onclick:GetAdvertisement),
                    new MenuElement("Найти профиль рекламодателя по названию", ConsoleColor.DarkCyan, onclick:GetAdvertiser),
                    new MenuElement("Добавить рекламное объявление", ConsoleColor.Green, onclick:AddAdvertisementAsync),
                    new MenuElement("Зарегистрировать рекламодателя", ConsoleColor.DarkGreen, onclick:RegisterAdvertiserAsync),
                    new MenuElement("Настройки учётной записи", ConsoleColor.Gray, accountSettings),
                    new MenuElement("Выйти из учётной записи", ConsoleColor.Red, onclick:Logout)
                }
            };
            if (Program.currentUser.IsAdministrator)
                menu.Elements.Add(new MenuElement("Инструменты администратора", onclick: UserManagement));
            return menu;
        }
        //Ниже идут действия по нажатию.
        private static Task ShowAdvertisementsAsync()
        {
            string selectionQueryPart = "SELECT * FROM advertisements ";
            Console.Write("Использовать фильтры для поиска? (y/n): ");
            bool filter;
            filter = string.Equals(Console.ReadLine(), "y", StringComparison.InvariantCultureIgnoreCase);
            void add(string where)
            {
                if (!selectionQueryPart.Contains("WHERE"))
                {
                    selectionQueryPart += "WHERE " + where;
                }
                else selectionQueryPart += "AND " + where;
            };
            if (filter)
            {
                Console.Write("Нужно ли показывать уже истёкшие объявления? (y/n)");
                if (!string.Equals(Console.ReadLine(), "y", StringComparison.InvariantCultureIgnoreCase))
                {
                    add($"CAST(expiry_date AS DATETIME) >= CAST(\"{DateTime.UtcNow:HH:mm dd:MM:yyyy}\" AS DATETIME) ");
                }
                Console.Write("Начальная дата регистрации для поиска объявлений (либо нажмите enter, чтобы пропустить): ");
                if (DateTime.TryParse(Console.ReadLine(), out var dt))
                {
                    add($"CAST(reg_date AS DATETIME) >= CAST(\"{dt:HH:mm dd:MM:yyyy}\" AS DATETIME) ");
                }
                Console.Write("Конечная дата регистрации для поиска объявлений (либо enter для пропуска): ");
                if (DateTime.TryParse(Console.ReadLine(), out dt))
                {
                    add($"CAST(reg_date AS DATETIME) <= CAST(\"{dt:HH:mm dd:MM:yyyy}\" AS DATETIME) ");
                }
            }
            Console.Write("Использовать обратный порядок сортировки? (y/n): ");
            selectionQueryPart += "ORDER BY id ";
            if (string.Equals(Console.ReadLine(), "y", StringComparison.InvariantCultureIgnoreCase))
            {
                selectionQueryPart += " DESC";
            }
            Console.Write("Введите ограничение на количество выводимых объявлений (не более 100): ");
            if (int.TryParse(Console.ReadLine(), out int limit) && limit <= 100)
            {
                selectionQueryPart += $"LIMIT {limit}";
            }
            else if (limit > 50 || limit < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Число превышает заданный лимит! Нажмите любую клавишу для выхода в главное меню.");
                Console.ReadKey();
                Program.ResetMenu();
            }
            selectionQueryPart += ';';
            SQLiteCommand cmd = new SQLiteCommand(selectionQueryPart, Program.connection);
            var reader = cmd.ExecuteReader();
            Console.Clear();
            Program.VizualizeTable(reader);
            return Task.CompletedTask;
        }

        public static async Task AddAdvertisementAsync()
        {
            Console.Write("Введите id или имя рекламодателя (можно найти на странице рекламодателя в приложении):");
            string input = Console.ReadLine();
            string query = "SELECT * FROM advertisers ";
            if (long.TryParse(input, out long advertiserID))
            {
                  query += $"WHERE id = {advertiserID} OR name = \"{input}\"";
            }
            else
            {
                query += $"WHERE name = \"{input}\"";
            }
            SQLiteCommand command = new SQLiteCommand(query, Program.connection);
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                long advertiserId = (long)reader["id"];
                Console.WriteLine("Введите URL объявления: ");
                string link = Console.ReadLine();
                Console.Write("Введите дату истечения рекламного контракта: ");
                if (!DateTime.TryParse(Console.ReadLine(), out var expDate))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Не получилось переконвертировать значение даты.");
                    Console.Write("Нажмите любой символ для выхода в главное меню...");
                    Program.ResetMenu();
                }
                SQLiteCommand writeAdv = new SQLiteCommand(
                    $"INSERT INTO advertisements (advertiser_id, link, reg_date, expiry_date)" +
                    $"VALUES ({advertiserId}, \"{link}\", \"{DateTime.UtcNow:HH:mm dd.MM.yyyy}\", \"{expDate:HH:mm dd.MM.yyyy}\");", 
                    Program.connection);
                await writeAdv.ExecuteNonQueryAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Запись успешно внесена! Нажмите любую клавишу для возврата в главное меню...");
                Console.ReadKey();
                Program.ResetMenu();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не было найдено учётной записи рекламодателя с такими учётными данными. Пожалуйста, попробуйте снова.");
                Console.Write("Нажмите любой символ для выхода в главное меню...");
                Console.ReadKey();
                Program.ResetMenu();
            }
        }

        public static async Task RegisterAdvertiserAsync()
        {
            Console.Write("Введите имя рекламодателя: ");
            string name = Console.ReadLine();
            Console.Write("Введите адрес рекламодателя: ");
            string address = Console.ReadLine();
            Console.Write("Введите email рекламодателя: ");
            string email = Console.ReadLine();
            if (!emailRegex.IsMatch(email))//Проверяем email
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Данная строка не является подходящим адресом электронной почты.");
                Console.Write("Нажмите любой символ для выхода в главное меню...");
                Console.ReadKey();
                Program.ResetMenu();
                return;
            }
            Console.Write("Введите URL рекламодателя: ");
            string link = Console.ReadLine();
            Console.Write("Введите номер телефона рекламодателя: ");
            string phone = Console.ReadLine();
            if (!phoneRegex.IsMatch(phone))//Проверяем email
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Данная строка не является подходящим номером телефона.");
                Console.Write("Нажмите любой символ для выхода в главное меню...");
                Console.ReadKey();
                Program.ResetMenu();
                return;
            }
            SQLiteCommand cmd = new SQLiteCommand(
                $"INSERT INTO advertisers (name, address, email, link, phone) VALUES (\"{name}\", \"{address}\", \"{email}\", \"{link}\", \"{phone}\");",
                Program.connection);
            await cmd.ExecuteNonQueryAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Рекламодатель успешно зарегистрирован. Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }

        public static Task ShowAdvertisersAsync()
        {
            string selectionQueryPart = "SELECT * FROM advertisers ";
            Console.Write("Использовать обратный порядок сортировки? (y/n): ");
            selectionQueryPart += "ORDER BY id ";
            if (string.Equals(Console.ReadLine(), "y", StringComparison.InvariantCultureIgnoreCase))
            {
                selectionQueryPart += " DESC";
            }
            Console.Write("Введите ограничение на количество выводимых записей (не более 100): ");
            if (int.TryParse(Console.ReadLine(), out int limit) && limit <= 100)
            {
                selectionQueryPart += $"LIMIT {limit}";
            }
            else if (limit > 50 || limit < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Число превышает заданный лимит! Нажмите любую клавишу для выхода в главное меню.");
                Console.ReadKey();
                Program.ResetMenu();
            }
            selectionQueryPart += ';';
            SQLiteCommand cmd = new SQLiteCommand(selectionQueryPart, Program.connection);
            var reader = cmd.ExecuteReader();
            Console.Clear();
            Program.VizualizeTable(reader);
            return Task.CompletedTask;
        }

        public static Task GetAdvertisement()
        {
            Console.Write("Введите часть адреса объявления для поиска: ");
            string name = Console.ReadLine();
            SQLiteCommand cmd = new SQLiteCommand(
                $"SELECT * FROM advertisements WHERE link LIKE \"%{name}%\"",
                Program.connection);
            var reader = cmd.ExecuteReader();
            Console.Clear();
            Program.VizualizeTable(reader);
            return Task.CompletedTask;
        }

        public static Task GetAdvertiser()
        {
            Console.Write("Введите id или имя рекламодателя (можно найти на странице рекламодателя в приложении):");
            string input = Console.ReadLine();
            string query = "SELECT * FROM advertisers ";
            if (long.TryParse(input, out long advertiserID))
            {
                query += $"WHERE id = {advertiserID} OR name = \"{input}\"";
            }
            else
            {
                query += $"WHERE name = \"{input}\"";
            }
            SQLiteCommand command = new SQLiteCommand(query, Program.connection);
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                long id = (long)reader["id"];
                var cmd = new SQLiteCommand(
                $"SELECT * FROM advertisements WHERE advertiser_id = {id}",
                Program.connection);
                reader = cmd.ExecuteReader();
                Console.Clear();
                Console.WriteLine("Показан список объявлений, которые связаны с этим рекламодателем:");
                Program.VizualizeTable(reader);
                return Task.CompletedTask;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Не удалось найти пользователя с этим именем. Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
            Program.ResetMenu();
            return Task.CompletedTask;
        }

        public static Task Logout()
        {
            Console.WriteLine("Работа с приложением успешно завершена. ");
            Program.connection.Close();
            Environment.Exit(0);
            return Task.CompletedTask;
        }

        public static async Task ChangeName()
        {
            Console.Write("Введите новый никнейм: ");
            string newName = Console.ReadLine();
            try
            {
                await Program.currentUser.ChangeUsernameAsync(newName);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Никнейм успешно изменён! Нажмите любую клавишу для возращения в главное меню: ");
                Console.ReadKey();
                Program.ResetMenu();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникла непредвиденная ошибка. Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
                Program.ResetMenu();
            }
        }

        public static async Task ChangePassword()
        {
            Console.Write("Введите старый пароль: ");
            string oldPassword = Console.ReadLine();
            Console.Write("Введите новый пароль: ");
            string newPasword = Console.ReadLine();
            try
            {
                await Program.currentUser.ChangePasswordAsync(oldPassword, newPasword);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Пароль успешно изменён! Нажмите любую клавишу для возращения в главное меню: ");
                Console.ReadKey();
                Program.ResetMenu();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникла непредвиденная ошибка. Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
                Program.ResetMenu();
            }
        }

        public static async Task UserManagement()
        {
            Console.WriteLine("Ниже представлен список всех пользователей: ");
            SQLiteCommand cmd = new SQLiteCommand(
                $"SELECT * FROM users",
                Program.connection);
            var reader = cmd.ExecuteReader();
            Program.VizualizeTable(reader, 1000000);
            Console.WriteLine();
            Console.Write("Введите id пользователя для управления (или Enter для выхода): ");
            long id;
            if (long.TryParse(Console.ReadLine(), out id))
            {
                string name = "";
                cmd = new SQLiteCommand($"SELECT * FROM users WHERE id = {id}", Program.connection);
                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    name = (string)reader["login"];
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Введён некорректный ID. Нажмите любую клавишу для выхода в главное меню...");
                    Console.ReadKey();
                    Program.ResetMenu();
                    return;
                }
                Console.WriteLine("Выберите действие с пользователем: ");
                Console.WriteLine("1 - Задать режим администратора");
                Console.WriteLine("2 - Сбросить пароль пользователя");
                Console.WriteLine("3 - Удалить учётную запись");
                Console.WriteLine("<Люой другой текст> - Вернуться в главное меню");
                string input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        {
                            Console.Write("Задайте режим администратора (true/false): ");
                            bool admin = bool.TryParse(Console.ReadLine(), out var a) && a;
                            Console.Write("Подтвердите, что вы Администратор (заново введите пароль): ");
                            string pass = Console.ReadLine();
                            string passHash = CLIUser.ComputeHashForPassword(pass);
                            if (passHash == Program.currentUser?.PasswordHash && Program.currentUser.IsAdministrator)
                            {
                                await CLIUser.SetAdministrator(name, admin, pass, Program.currentUser);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Вы не подтвердили пароль от учётной записи Администратора. Нажмите любую клавишу для выхода в главное меню...");
                                Console.ReadKey();
                                Program.ResetMenu();
                            }
                            break;
                        }
                    case "2":
                        {
                            if (name == Program.currentUser?.Username)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Вы не можете сбросить пароль от собственной учётной записи. Нажмите любую клавишу для выхода в главное меню...");
                                Console.ReadKey();
                                Program.ResetMenu();
                                return;
                            }
                            Console.Write("Подтвердите, что вы Администратор (заново введите пароль): ");
                            string pass = Console.ReadLine();
                            string passHash = CLIUser.ComputeHashForPassword(pass);
                            if (passHash == Program.currentUser?.PasswordHash && Program.currentUser.IsAdministrator)
                            {
                                string newPassHash = CLIUser.ComputeHashForPassword("12345");
                                SQLiteCommand upd = new SQLiteCommand($"UPDATE users" +
                                    $"SET password_hash = {newPassHash} WHERE login LIKE \"{name}%\"", Program.connection);
                                upd.ExecuteNonQuery();
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Пароль успешно изменён на 12345! Нажмите любую клавишу для возращения в главное меню: ");
                                Console.ReadKey();
                                Program.ResetMenu();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Вы не подтвердили пароль от учётной записи Администратора. Нажмите любую клавишу для выхода в главное меню...");
                                Console.ReadKey();
                                Program.ResetMenu();
                            }
                            break;
                        }
                    case "3":
                        {
                            if (name == Program.currentUser?.Username)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Вы не можете удалить собственную учётную запись. Нажмите любую клавишу для выхода в главное меню...");
                                Console.ReadKey();
                                Program.ResetMenu();
                                return;
                            }
                            Console.Write("Подтвердите, что вы Администратор (заново введите пароль): ");
                            string pass = Console.ReadLine();
                            string passHash = CLIUser.ComputeHashForPassword(pass);
                            if (passHash == Program.currentUser?.PasswordHash && Program.currentUser.IsAdministrator)
                            {
                                SQLiteCommand upd = new SQLiteCommand($"DELETE FROM users WHERE id = {id}", Program.connection);
                                int v = upd.ExecuteNonQuery();
                                if (v != 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Пользователь успешно удалён из системы! Нажмите любую клавишу для возращения в главное меню: ");
                                    Console.ReadKey();
                                    Program.ResetMenu();
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("На нашлось записи с указанными данными. Нажмите любую клавишу для выхода в главное меню...");
                                    Console.ReadKey();
                                    Program.ResetMenu();
                                }
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Вы не подтвердили пароль от учётной записи Администратора. Нажмите любую клавишу для выхода в главное меню...");
                                Console.ReadKey();
                                Program.ResetMenu();
                            }
                            break;
                        }
                }
            }
            return;
        }
    }
}
