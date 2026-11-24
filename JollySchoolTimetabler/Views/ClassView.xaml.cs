using System.Windows.Controls;
using JollySchoolTimetabler.ViewModels;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for ClassView.xaml
    /// </summary>
    public partial class ClassView : UserControl
    {
        public ClassView()
        {
            InitializeComponent();
            DataContext = new ClassViewModel(App.DbContext);
        }
    }
}
