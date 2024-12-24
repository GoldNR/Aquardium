namespace Aquardium
{
    public partial class AppShell : Shell
    {
        public static MainPage CurrentMainPage { get; set; }

        public static DashboardPage CurrentDashboardPage { get; set; }

        private HashSet<string> _registeredRoutes = new HashSet<string>();

        public AppShell()
        {
            InitializeComponent();
        }

        public void RegisterArduino(ArduinoDevice arduino)
        {
            var route = $"{arduino.Name}";

            // Check if the route is already registered
            if (!_registeredRoutes.Contains(route))
            {
                Routing.RegisterRoute(route, typeof(DashboardPage));
                _registeredRoutes.Add(route);
            }

            // Create FlyoutItem
            var flyoutItem = new FlyoutItem
            {
                Title = arduino.Name,
                Route = route,
                FlyoutDisplayOptions = FlyoutDisplayOptions.AsMultipleItems,
                Items =
                {
                    new ShellContent
                    {
                        Content = new DashboardPage { SelectedArduino = arduino },
                        Route = route
                    }
                }
            };

            // Add FlyoutItem to Shell
            if (!Shell.Current.Items.Any(x => x.Title == arduino.Name))
            {
                Shell.Current.Items.Add(flyoutItem);
                Console.WriteLine($"Flyout item added: {arduino.Name}");
            }
            else
            {
                Console.WriteLine($"Flyout item already exists: {arduino.Name}");
            }
        }


        public ShellItem FindFlyoutItemByTitle(string title)
        {
            foreach (var item in Shell.Current.Items)
            {
                // Direct match for FlyoutItem or ShellItem
                if (item.Title == title)
                {
                    return item;
                }

                // Check nested ShellSections within ShellItem
                if (item is ShellItem shellItem)
                {
                    var foundItem = FindInShellItem(shellItem, title);
                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
            }
            return null;
        }

        // Helper to check within ShellItem's sections and contents
        private ShellItem FindInShellItem(ShellItem shellItem, string title)
        {
            foreach (var section in shellItem.Items)
            {
                // Match at ShellSection level
                if (section.Title == title)
                {
                    return shellItem;
                }

                // Match within ShellContent (pages)
                foreach (var content in section.Items)
                {
                    if (content.Title == title)
                    {
                        return shellItem;
                    }
                }
            }
            return null;
        }




    }
}
