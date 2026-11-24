using System.Windows.Controls;
using JollySchoolTimetabler.ViewModels;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for SubjectView.xaml
    /// </summary>
    public partial class SubjectView : UserControl
    {
        public SubjectView()
        {
            InitializeComponent();
            DataContext = new SubjectViewModel(App.DbContext);
        }
    }
}
