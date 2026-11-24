using System.Windows;
using JollySchoolTimetabler.ViewModels;

namespace JollySchoolTimetabler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new DatabaseViewModel();
        }
    }
}