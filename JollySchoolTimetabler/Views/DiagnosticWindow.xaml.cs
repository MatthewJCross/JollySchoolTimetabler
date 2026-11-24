using System.Windows;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for DiagnosticWindow.xaml
    /// </summary>
    public partial class DiagnosticWindow : Window
    {
        public DiagnosticWindow(object viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
