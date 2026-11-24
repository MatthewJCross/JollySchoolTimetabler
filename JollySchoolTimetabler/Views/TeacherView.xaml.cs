using JollySchoolTimetabler.ViewModels;
using System.Windows.Controls;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for TeacherView.xaml
    /// </summary>
    public partial class TeacherView : UserControl
    {
        public TeacherView()
        {
            InitializeComponent();
            DataContext = new TeacherViewModel(App.DbContext);
        }
    }
}
