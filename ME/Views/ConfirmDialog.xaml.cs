using System.Windows;

namespace ME.Views
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, string message, string confirmText = "确定", string cancelText = "取消")
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmBtn.Content = confirmText;
            CancelBtn.Content = cancelText;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public static bool Show(Window owner, string title, string message, string confirmText = "确定", string cancelText = "取消")
        {
            var dialog = new ConfirmDialog(title, message, confirmText, cancelText) { Owner = owner };
            return dialog.ShowDialog() == true;
        }
    }
}
