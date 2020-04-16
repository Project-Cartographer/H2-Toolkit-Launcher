using System.Windows;

namespace CustomOkDialogWPF
{
    /// <summary>
    /// Interaction logic for MsgBoxOk.xaml
    /// </summary>
    public partial class MsgBoxOk : Window
    {
        public MsgBoxOk(string message, string header, string ok)
        {
            InitializeComponent();

            this.Title = header;
            content.Text = message;
            Ok.Content = ok;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

    }
}