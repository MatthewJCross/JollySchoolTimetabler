using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Utils;
using System.IO;
using System.Windows;

namespace JollySchoolTimetabler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const int SEED = 0;
        
        public static SchoolContext DbContext { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Enable mouse wheel scrolling globally for all ListBoxes
            GlobalListBoxScrollHelper.EnableGlobalListBoxScrolling();

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "school.db");
            // Optional: Delete the existing DB file for a completely clean reset
            if (SEED > 0)
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    Console.WriteLine("Old database deleted.");
                }
            }

            DbContext = new SchoolContext();
            DbContext.Database.EnsureCreated(); // creates db if missing

            if (SEED > 0)
                SeedData.Initialize(DbContext);
        }
    }
}
