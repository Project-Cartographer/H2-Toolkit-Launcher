using System.Windows;

namespace CustomYesNoDialogWPF
{
    /// <summary>
    /// Interaction logic for MsgBoxYesNo.xaml
    /// </summary>
    public partial class MsgBoxYesNo : Window
    {
        public MsgBoxYesNo(string message, string header, string yes, string no)
        {
            InitializeComponent();

            this.Title = header;
            content.Text = message;
            Yes.Content = yes;
            No.Content = no;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }


    }
}