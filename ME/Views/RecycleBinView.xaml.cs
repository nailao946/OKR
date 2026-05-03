using System.Windows.Controls;
using ME.ViewModels;

namespace ME.Views
{
    public partial class RecycleBinView : UserControl
    {
        public RecycleBinView()
        {
            InitializeComponent();
            DataContext = new RecycleBinViewModel();
        }
    }
}
