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
        /// <summary>
        /// Смена никнейма пользователя.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public async Task ChangeUsernameAsync(string newName)
        {
            SQLiteCommand upd = new SQLiteCommand($"UPDATE users" +
                $"SET login = {newName} WHERE login LIKE \"{Username}%\"", Program.connection);
            await upd.ExecuteNonQueryAsync();
            Username = newName;
        }
        /// <summary>
        /// Смена пароля. Для "безопсаности" буду требовать старый пароль.
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        /// <exception cref="AccessViolationException">Будет выбрасываться в случае, если пароль неверный (не знал, какое исключение придумать).</exception>
        /// <exception cref="ArgumentException">Если не нашлось такого никнейма.</exception>
        public async Task ChangePasswordAsync(string oldPassword, string newPassword)
        {
            var md5 = MD5.Create();
            string hash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(newPassword)));
            string oldHash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(oldPassword)));
            if (oldHash != PasswordHash)
                throw new AccessViolationException("Wrong password!");
            SQLiteCommand upd = new SQLiteCommand($"UPDATE users" +
                $"SET password_hash = {hash} WHERE password_hash = \"{oldHash}\" AND login LIKE \"{Username}%\"", Program.connection);
            int v = await upd.ExecuteNonQueryAsync();
            if (v == 0)
                throw new ArgumentException($"There's no elements in table with username {Username} or old password was wrong!", "username");
            PasswordHash = hash;
        }
        /// <summary>
        /// Создаёт запись о новом пользователе.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Задаёт права администратора для пользователя приложением.
        /// </summary>
        /// <param name="username">Его никнейм.</param>
        /// <param name="value">Значение для прав администратора (включить/отключить).</param>
        /// <param name="currentSessionPass">Пароль от текущей сессии у аккаунта администратора.</param>
        /// <param name="initiator">Учётная запись того, кто изменяет права.</param>
        /// <returns></returns>
        /// <exception cref="AccessViolationException">Опять же, не знал, что выкидывать в случае неправильного пароля или превышения прав.</exception>
        /// <exception cref="ArgumentException">Не нашлось записи о пользователе.</exception>
        public static Task SetAdministrator(string username, bool value, string currentSessionPass, CLIUser initiator)
        {
            if (ComputeHashForPassword(currentSessionPass) != initiator.PasswordHash)
                throw new AccessViolationException($"Wrong password from administrator login!");
            if (!initiator.IsAdministrator)
                throw new AccessViolationException("This login has not administrator rights!");
            SQLiteCommand cmd = new SQLiteCommand($"UPDATE users " +
                $"SET admin = {value} WHERE login = \"{username}\";", Program.connection);
            int v = cmd.ExecuteNonQuery();
            if (v == 0) throw new ArgumentException($"There's no elements in table with username {username}!", "username");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Осуществляет расчёт MD5-хеша для пароля (более сложного алгоритма не стал искать).
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static string ComputeHashForPassword(string pass)
        {
            var md5 = MD5.Create();
            string hash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(pass)));
            return hash;
        }
    }
}
