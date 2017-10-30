using Microsoft.Win32;
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
using System.IO;
using static System.Environment;
using static System.Object;
using static System.Diagnostics.Process;

namespace Halo2CodezLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string H2Ek_install_path = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Microsoft Games\\Halo 2 Map Editor\\";
        private string Halo_install_path = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Microsoft Games\\Halo 2\\";
        bool show_light_slider = false;
        public MainWindow()
        {
            H2Ek_install_path = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Halo 2", "tools_directory", H2Ek_install_path).ToString();
            InitializeComponent();
        }
        private void RunHalo2Sapien(object sender, RoutedEventArgs e)
        {
           Start(H2Ek_install_path + "h2sapien.exe");
        }
        private void RunHalo2Guerilla(object sender, RoutedEventArgs e)
        {
            Start(H2Ek_install_path + "h2guerilla.exe");
        }
        private void RunHalo2(object sender, RoutedEventArgs e)
        {
            Start(Halo_install_path + "halo2.exe");
        }

        private void ResetSapienDisplay(object sender, RoutedEventArgs e)
        {
            RegistryKey myKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\bungie\\sapien", true);
            if (myKey != null)
            {
                myKey.DeleteValue("sapien_loading_window_rect", false);
                myKey.DeleteValue("warnings_window_rect", false);
                myKey.DeleteValue("main_window_frame", false);
                myKey.DeleteValue("output_window_rect", false);
                myKey.DeleteValue("options_window_rect", false);
                myKey.DeleteValue("hierarchy_window_rect", false);
                myKey.DeleteValue("property_window_rect", false);
                myKey.DeleteValue("main_window_rect", false);
                myKey.DeleteValue("type_list_rect", false);
                myKey.Close();
            }
        }

        private void CompileLevel(object sender, RoutedEventArgs e)
        {
            string level_path = compile_level_path.ToString();
            if (File.Exists(level_path))
            {
                MessageBox.Show("Not Implemented yet!");
                Start(H2Ek_install_path + "h2tool.exe", "bitmaps \"" + level_path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void CompileText(object sender, RoutedEventArgs e)
        {
            string text_path = compile_text_path.ToString();
            if (File.Exists(text_path))
            {
                Start(H2Ek_install_path + "h2tool.exe", "new-strings \"" + text_path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void CompileImage(object sender, RoutedEventArgs e)
        {
            string image_path = compile_image_path.ToString();
            if (File.Exists(image_path))
            {
                Start(H2Ek_install_path + "h2tool.exe", "bitmaps \"" + image_path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void PackageLevel(object sender, RoutedEventArgs e)
        {
            return;
            string level_path = package_level_path.ToString();
            if (File.Exists(level_path))
            {
                level_path.Replace(".scenario", "");
                Start(H2Ek_install_path + "h2tool.exe", "build-cache-file \"" + level_path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void CompileOnly_Checked(object sender, RoutedEventArgs e)
        {
            show_light_slider = false;
        }

        private void LightOnly_Checked(object sender, RoutedEventArgs e)
        {
            show_light_slider = true;
        }

        private void CompileAndLight_Checked(object sender, RoutedEventArgs e)
        {
            show_light_slider = true;
        }

        private void browse_level_compile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented yet!");
        }

        private void Browse_text_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented yet!");
        }

        private void browse_bitmap_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented yet!");
        }

        private void browse_package_level_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented yet!");
        }
    }

    public class TextInputToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Always test MultiValueConverter inputs for non-null 
            // (to avoid crash bugs for views in the designer) 
            if (values[0] is bool && values[1] is bool)
            {
                bool hasText = !(bool)values[0];
                bool hasFocus = (bool)values[1];
                if (hasFocus || hasText)
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
