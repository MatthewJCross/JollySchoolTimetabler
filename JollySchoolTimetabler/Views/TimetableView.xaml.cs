using System.Windows.Controls;
using JollySchoolTimetabler.ViewModels;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for TimetableView.xaml
    /// </summary>
    public partial class TimetableView : UserControl
    {
        public TimetableView()
        {
            InitializeComponent();
            // Inherit the shared TimetableViewModel from MainWindow.
            // Creating a second view model here disconnects list selection from the grid.
        }
    }
}
