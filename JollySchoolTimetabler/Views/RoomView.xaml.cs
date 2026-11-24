using System.Windows.Controls;
using JollySchoolTimetabler.ViewModels;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for RoomView.xaml
    /// </summary>
    public partial class RoomView : UserControl
    {
        public RoomView()
        {
            InitializeComponent();
            DataContext = new RoomViewModel(App.DbContext);
        }
    }
}
