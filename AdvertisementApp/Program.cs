using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdvertisementApp
{
    class Program
    {
        /// <summary>
        /// Сохраним SQL-соединение и откроем доступ из других файлов.
        /// </summary>
        internal static SQLiteConnection connection;
        /// <summary>
        /// Это поле отвечает за текущее выбранное меню.
        /// </summary>
        static ConsoleMenu currentMenu;
        /// <summary>
        /// Это поле сохраняет текущего пользователя в данной сессии.
        /// </summary>
        internal static CLIUser currentUser;
        /// <summary>
        /// Главное меню будет храниться всегда для быстрого возврата.
        /// </summary>
        public static ConsoleMenu MainMenu;

        static void Main(string[] args)
        {
            if (!File.Exists("adv.db"))//Создаём БД, если она ещё не существует.
            {
                File.Create("adv.db").Close();
            }
            connection = new SQLiteConnection("Data SOURCE=adv.db");//Создаём соединение.
            try
            {
                connection.Open();//Открываем соединение.
                //SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS advertisements (id INTEGER PRIMARY KEY AUTOINCREMENT) ", connection);
                //SQLiteCommand command1 = new SQLiteCommand("CREATE TABLE IF NOT EXISTS advertisers (id INTEGER PRIMARY KEY AUTOINCREMENT)", connection);
                //SQLiteCommand command2 = new SQLiteCommand("INSERT INTO advertisers VALUES (1)", connection);
                //SQLiteCommand command3 = new SQLiteCommand("SELECT * FROM advertisers", connection);
                //SQLiteDataReader reader;
                //command.ExecuteNonQuery();
                //command1.ExecuteNonQuery();
                //command2.ExecuteNonQuery();
                //reader = command3.ExecuteReader();
                //reader.Read();
                //Console.WriteLine(reader["id"]);
                InitializeDatabase();//Первичная настройка БД
                MainTask().GetAwaiter().GetResult();//Запускаю основную задачу (наверное я зря сделал её асинхронной, в данном случае от этого вообще 0 смысла, но всё же).
            }
            catch
            {
                throw;
            }
            finally
            {
                connection.Close();
            }

        }

        public static void SwapMenu(ConsoleMenu newMenu)
        {
            currentMenu = newMenu;
        }

        static async Task MainTask()
        {
            bool l;
            Console.WriteLine("Добро пожаловать в AdvertiseMe NT CLI!");
            do//Процесс входа в аккаунт.
            {
                Console.Write("Пожалуйста, введите логин: ");
                string login = Console.ReadLine();
                Console.Write("Введите пароль: ");
                string password = Console.ReadLine();
                l = !await LoginUser(login, password);
            } while (l);
            MainMenu = MenuBuildService.BuildMenuSystem();
            currentMenu = MainMenu;
            do//Основной цикл в сессии. Осуществляет управление меню и рабоотает с интерфейсом.
            {
                currentMenu.RedrawMenu();
                Console.WriteLine("Нажимайте клавиши со стрелками для навигации или введите номер опции...");
                if (!Console.KeyAvailable) SpinWait.SpinUntil(() => Console.KeyAvailable);
                Thread.Sleep(150);
                var input = Console.ReadKey();
                if (input.KeyChar >= '0' && input.KeyChar <= '9')//Основные кнопки для управления: цифры, вверх-вниз, Enter и Escape.
                {
                    int pos = Convert.ToInt32(Convert.ToString(input.KeyChar));
                    if (pos < currentMenu.Elements.Count) currentMenu.PointerPosition = pos;
                    else currentMenu.PointerPosition = 0;
                }
                else switch (input.Key)
                {
                    case ConsoleKey.UpArrow:
                        {
                            currentMenu.PointerPosition--;
                            if (currentMenu.PointerPosition < 0)
                                currentMenu.PointerPosition = currentMenu.Elements.Count - 1;
                            break;
                        }
                    case ConsoleKey.DownArrow:
                        {
                            currentMenu.PointerPosition++;
                            if (currentMenu.PointerPosition >= currentMenu.Elements.Count)
                                currentMenu.PointerPosition = 0;
                            break;
                        }
                    case ConsoleKey.Enter:
                        {
                            await currentMenu.InitiateClickAsync();
                            break;
                        }
                    case ConsoleKey.Escape:
                        {
                            currentMenu = MainMenu;
                            break;
                        }
                }
            }
            while (true);
        }

        static void InitializeDatabase()
        {
            SQLiteCommand init = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS advertisements (id INTEGER PRIMARY KEY AUTOINCREMENT, advertiser_id INTEGER, link TEXT, expiry_date TEXT, reg_date TEXT);" +
                "CREATE TABLE IF NOT EXISTS advertisers (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, address TEXT UNIQUE, email VARCHAR(32) UNIQUE, link TEXT UNIQUE, phone VARCHAR(12) UNIQUE);" +
                "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY AUTOINCREMENT, login VARCHAR(16) UNIQUE, password_hash TEXT, admin BOOLEAN);", connection);
            init.ExecuteNonQuery();
        }
        /// <summary>
        /// Функция для осуществления входа в учётную запись.
        /// </summary>
        /// <param name="name">Логин</param>
        /// <param name="password">Пароль</param>
        /// <returns></returns>
        internal static async Task<bool> LoginUser(string name, string password)
        {
            SQLiteCommand check = new SQLiteCommand($"SELECT * FROM users WHERE login LIKE \"{name}%\";", connection);
            var reader = await check.ExecuteReaderAsync();
            bool s = reader.Read();
            if (s)
            {
                string passHash = (string)reader["password_hash"];
                if (CLIUser.ComputeHashForPassword(password) == passHash)
                    currentUser = new CLIUser(reader["login"] as string, reader["password_hash"] as string, (bool)reader["admin"]);
                else
                {
                    Console.WriteLine("Введён неверный пароль! Нажмите любую клавишу, чтобы повторить попытку: ");
                    Console.ReadKey();
                    return false;
                }
            }
            else if (!s)
            {
                Console.Write("Данной учётной записи ещё не существует. Нажмите Y, чтобы создать новую с этими данными:");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    currentUser = await CLIUser.RegisterNewUser(name, password);
                }
                else
                {
                    Console.Clear();
                    return false;
                }
            }
            return true;
        }

        public static void ResetMenu()
        {
            Console.Clear();
            Console.ResetColor();
            currentMenu = MainMenu;
        }
        /// <summary>
        /// Осуществляет отрисовку таблицы на экране.
        /// </summary>
        /// <param name="reader">Созданный SQL-запросом <see cref="SQLiteDataReader"/>.</param>
        /// <param name="limit">Лимит на число отрисовываемых записей.</param>
        /// <param name="width">Ширина одного столбца.</param>
        public static void VizualizeTable(SQLiteDataReader reader, int limit = 100, int width = 20)
        {
            Console.WriteLine("Визуализация таблицы по запросу... (Показано не более 50 результатов)");
            int columns = reader.FieldCount;
            int r = 0;
            //Этап 1. Рисуем верхнюю границу.
            Console.Write("╔");
            for (int c = 0; c < Math.Min(columns, width); c++)
            {
                Console.Write(string.Join("", Enumerable.Repeat("═", width)));
                if (c != Math.Min(columns, width) - 1) Console.Write("╦");
            }
            Console.WriteLine("╗");
            string minimize(string seq)
            {
                if (seq.Length > width) return seq[..(width - 1)] + "…";
                return seq + string.Join("", Enumerable.Repeat(" ", width - seq.Length));
            }
            //Этап 2. Выводим названия заголовков.
            Console.Write("║");
            for (int c = 0; c < Math.Min(columns, width); c++)
            {
                Console.Write(minimize(reader.GetOriginalName(c)) + "║");
            }
            Console.WriteLine();
            //Этап 3. Рисуем разделитель между заголовком и записями.
            Console.Write("╠");
            for (int c = 0; c < Math.Min(columns, width); c++)
            {
                Console.Write(string.Join("", Enumerable.Repeat("═", width)));
                if (c != Math.Min(columns, width) - 1) Console.Write("╫");
            }
            Console.WriteLine("╢");
            //Этап 4. Рисуем записи.
            while (reader.Read() && r++ < limit)
            {
                Console.Write("║");
                for (int c = 0; c < columns; c++)
                {
                    var e = reader.GetFieldType(c);
                    string val;
                    if (e == typeof(DateTime))
                    {
                        val = reader.GetDateTime(c).ToString();
                    }
                    else val = reader[c].ToString();
                    Console.Write(minimize(val));
                    if (c != Math.Min(columns, width) - 1) Console.Write("║");
                }
                Console.WriteLine("║");
            }
            Console.Write("╚");
            for (int c = 0; c < Math.Min(columns, width); c++)
            {
                Console.Write(string.Join("", Enumerable.Repeat("═", width)));
                if (c != Math.Min(columns, width) - 1) Console.Write("╩");
            }
            Console.WriteLine("╝");
            Console.Write("Нажмите ESC для выхода из режима просмотра таблицы: ");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }
        }
    }
}
