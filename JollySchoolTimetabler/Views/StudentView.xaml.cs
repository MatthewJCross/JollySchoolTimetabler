using System.Windows.Controls;
using JollySchoolTimetabler.ViewModels;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for StudentView.xaml
    /// </summary>
    public partial class StudentView : UserControl
    {
        public StudentView()
        {
            InitializeComponent();
            DataContext = new StudentViewModel(App.DbContext);
        }
    }
}
