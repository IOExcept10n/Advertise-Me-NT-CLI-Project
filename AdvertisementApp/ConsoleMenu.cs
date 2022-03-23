using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AdvertisementApp
{
    public class ConsoleMenu
    {
        public List<MenuElement> Elements { get; set; }

        public string Description { get; set; }

        public int PointerPosition { get; set; }

        public ConsoleMenu(string desc)
        {
            Description = desc;
        }

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

        public async Task InitiateClickAsync()
        {
            await Elements[PointerPosition].ClickAsync();
        }
    }

    public class MenuElement
    {

        public ConsoleColor Color { get; set; }

        public ConsoleMenu OwnerMenu { get; set; }

        public string Name { get; set; }

        public ConsoleMenu NextMenu { get; set; }

        public MenuElement(string name, ConsoleColor color = ConsoleColor.White, ConsoleMenu nextMenu = null, Func<Task> onclick = null)
        {
            Name = name;
            Color = color;
            NextMenu = nextMenu;
            OnClickCustomAction = onclick;
        }

        public Func<Task> OnClickCustomAction { get; set; }

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
