using JollySchoolTimetabler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JollySchoolTimetabler.Views
{
    /// <summary>
    /// Interaction logic for YearGroupView.xaml
    /// </summary>
    public partial class YearGroupView : UserControl
    {
        public YearGroupView()
        {
            InitializeComponent();
            DataContext = new YearGroupViewModel(App.DbContext);
        }
    }
}
