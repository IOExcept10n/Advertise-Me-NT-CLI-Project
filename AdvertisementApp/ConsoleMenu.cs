using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AdvertisementApp
{
    public class ConsoleMenu
    {
        /// <summary>
        /// Список элементов меню.
        /// </summary>
        public List<MenuElement> Elements { get; set; }
        /// <summary>
        /// Описание меню (текст, который будет выведен пользователю).
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Положение курсора.
        /// </summary>
        public int PointerPosition { get; set; }

        public ConsoleMenu(string desc)
        {
            Description = desc;
        }
        /// <summary>
        /// Обновить меню и отрисовать его заново.
        /// </summary>
        public void RedrawMenu()
        {
            Console.Clear();
            Console.WriteLine(Description);
            int i = 0;
            foreach (var elem in Elements)
            {
                Console.ForegroundColor = elem.Color;
                Console.WriteLine($"{(PointerPosition == i ? '>' : ' ')}{i++}: {elem.Name}");
                Console.ResetColor();
            }
        }

        public void Initialize()
        {
            foreach (var e in Elements)
            {
                if (e.OwnerMenu != null) continue;
                e.OwnerMenu = this;
                e.NextMenu?.Initialize();
            }
        }
        /// <summary>
        /// Осуществить нажаите на элемент меню.
        /// </summary>
        /// <returns></returns>
        public async Task InitiateClickAsync()
        {
            await Elements[PointerPosition].ClickAsync();
        }
    }
    /// <summary>
    /// Класс, предстваляющий собой элемент меню.
    /// </summary>
    public class MenuElement
    {
        /// <summary>
        /// Цвет элемента.
        /// </summary>
        public ConsoleColor Color { get; set; }
        /// <summary>
        /// Меню-владелец.
        /// </summary>
        public ConsoleMenu OwnerMenu { get; set; }
        /// <summary>
        /// Текст на элементе.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Меню, на которое будет осщуествлён переход в случае нажатия.
        /// </summary>
        public ConsoleMenu NextMenu { get; set; }

        public MenuElement(string name, ConsoleColor color = ConsoleColor.White, ConsoleMenu nextMenu = null, Func<Task> onclick = null)
        {
            Name = name;
            Color = color;
            NextMenu = nextMenu;
            OnClickCustomAction = onclick;
        }
        /// <summary>
        /// Функция, которая будет выполнена в результате нажатия.
        /// </summary>
        public Func<Task> OnClickCustomAction { get; set; }
        /// <summary>
        /// Действие по нажатию.
        /// </summary>
        /// <returns></returns>
        public async Task ClickAsync()
        {
            if (NextMenu != null)
            {
                Program.SwapMenu(NextMenu);
            }
            if (OnClickCustomAction != null)
            {
                try
                {
                    Console.Clear();
                    await OnClickCustomAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }
    }
}
