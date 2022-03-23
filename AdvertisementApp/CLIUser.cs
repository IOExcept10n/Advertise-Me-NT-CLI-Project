using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AdvertisementApp
{
    class CLIUser
    {
        public string Username { get; private set; }
        //По-хорошему, здесь должна быть нормальная функция сохранения пароля, но она не была реализована из-за недостатка времени
        //Поэтому, вместо шифрования пароля и нормального вычисления хеша здесь будет хеш md5.
        public string PasswordHash { get; private set; }

        public bool IsAdministrator { get; private set; }

        private CLIUser()
        {

        }

        public CLIUser(string username, string passHash, bool isAdmin = false)
        {
            Username = username;
            PasswordHash = passHash;
            IsAdministrator = isAdmin;
        }

        public async Task ChangeUsernameAsync(string newName)
        {
            SQLiteCommand upd = new SQLiteCommand($"UPDATE users" +
                $"SET login = {newName} WHERE login LIKE \"{Username}%\"", Program.connection);
            await upd.ExecuteNonQueryAsync();
            Username = newName;
        }

        public async Task ChangePasswordAsync(string oldPassword, string newPassword)
        {
            var md5 = MD5.Create();
            string hash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(newPassword)));
            string oldHash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(oldPassword)));
            if (oldHash != PasswordHash)
                throw new AccessViolationException("Wrong password!");
            SQLiteCommand upd = new SQLiteCommand($"UPDATE users" +
                $"SET login = {hash} WHERE password_hash = \"{oldHash}\" AND login LIKE \"{Username}%\"", Program.connection);
            int v = await upd.ExecuteNonQueryAsync();
            if (v == 0)
                throw new ArgumentException($"There's no elements in table with username {Username} or old password was wrong!", "username");
            PasswordHash = hash;
        }

        public static async Task<CLIUser> RegisterNewUser(string name, string password)
        {
            CLIUser user = new CLIUser();
            user.Username = name;
            user.PasswordHash = ComputeHashForPassword(password);
            user.IsAdministrator = name == "admin" && password == "admin";
            SQLiteCommand create = new SQLiteCommand($"INSERT INTO users (login, password_hash, admin) VALUES (\"{name}\", \"{ComputeHashForPassword(password)}\", {user.IsAdministrator});", Program.connection);
            await create.ExecuteNonQueryAsync();
            return user;
        }

        public async Task SetAdministratorAsync(string username, string value, string currentSessionPass, CLIUser initiator)
        {
            if (ComputeHashForPassword(currentSessionPass) != initiator.PasswordHash)
                throw new AccessViolationException($"Wrong password from administrator login!");
            if (!initiator.IsAdministrator)
                throw new AccessViolationException("This login has not administrator rights!");
            SQLiteCommand cmd = new SQLiteCommand($"UPDATE users" +
                $"SET admin = \"{value}\" WHERE login = \"{username}\";", Program.connection);
            int v = await cmd.ExecuteNonQueryAsync();
            if (v == 0) throw new ArgumentException($"There's no elements in table with username {username}!", "username");
        }


        public static string ComputeHashForPassword(string pass)
        {
            var md5 = MD5.Create();
            string hash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(pass)));
            return hash;
        }
    }
}
