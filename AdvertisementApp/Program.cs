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
        internal static SQLiteConnection connection;

        static ConsoleMenu currentMenu;

        internal static CLIUser currentUser;

        public static ConsoleMenu MainMenu;

        static void Main(string[] args)
        {
            if (!File.Exists("adv.db"))
            {
                File.Create("adv.db").Close();
            }
            connection = new SQLiteConnection("Data SOURCE=adv.db");
            try
            {
                connection.Open();
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
                InitializeDatabase();
                MainTask().GetAwaiter().GetResult();
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
            do
            {
                Console.Write("Пожалуйста, введите логин: ");
                string login = Console.ReadLine();
                Console.Write("Введите пароль: ");
                string password = Console.ReadLine();
                l = !await LoginUser(login, password);
            } while (l);
            MainMenu = MenuBuildService.BuildMenuSystem();
            currentMenu = MainMenu;
            do
            {
                currentMenu.RedrawMenu();
                Console.WriteLine("Нажимайте клавиши со стрелками для навигации или введите номер опции...");
                if (!Console.KeyAvailable) SpinWait.SpinUntil(() => Console.KeyAvailable);
                Thread.Sleep(150);
                var input = Console.ReadKey();
                if (input.KeyChar >= '0' && input.KeyChar <= '9')
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

        public static void VizualizeTable(SQLiteDataReader reader, int limit = 100)
        {
            Console.WriteLine("Визуализация таблицы по запросу... (Показано не более 50 результатов)");
            int columns = reader.FieldCount;
            int r = 0;
            Console.Write("╔");
            for (int c = 0; c < Math.Min(columns, 15); c++)
            {
                Console.Write(string.Join("", Enumerable.Repeat("═", 15)));
                if (c != Math.Min(columns, 15) - 1) Console.Write("╦");
            }
            Console.WriteLine("╗");
            string minimize(string seq)
            {
                if (seq.Length > 15) return seq.Substring(0, 14) + "…";
                return seq + string.Join("", Enumerable.Repeat(" ", 15 - seq.Length));
            }
            Console.Write("║");
            for (int c = 0; c < Math.Min(columns, 15); c++)
            {
                Console.Write(minimize(reader.GetOriginalName(c)) + "║");
            }
            Console.WriteLine();
            Console.Write("╠");
            for (int c = 0; c < Math.Min(columns, 15); c++)
            {
                Console.Write(string.Join("", Enumerable.Repeat("═", 15)));
                if (c != Math.Min(columns, 15) - 1) Console.Write("╫");
            }
            Console.WriteLine("╢");
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
                    if (c != Math.Min(columns, 15) - 1) Console.Write("║");
                }
                Console.WriteLine("║");
            }
            Console.Write("╚");
            for (int c = 0; c < Math.Min(columns, 15); c++)
            {
                Console.Write(string.Join("", Enumerable.Repeat("═", 15)));
                if (c != Math.Min(columns, 15) - 1) Console.Write("╩");
            }
            Console.WriteLine("╝");
            Console.Write("Нажмите ESC для выхода из режима просмотра таблицы: ");
            while (Console.ReadKey(false).Key != ConsoleKey.Escape)
            {
            }
        }
    }
}
