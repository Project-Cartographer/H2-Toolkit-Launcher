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
using System.Threading;
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
        [Flags] enum level_compile_type : Byte
        {
            none = 0,
            compile = 2,
            light = 4,
        }
        level_compile_type levelCompileType;

        enum light_quality
        {
            checkerboard,
            direct_only,
            draft_low,
            draft_medium,
            draft_high,
            low,
            medium,
            high,
            super
        }

        enum object_type
        {
            biped,
            vehicle,
            weapon,
            equipment,
            garbage,
            projectile,
            scenery,
            machine,
            control,
            light_fixture,
            sound_scenery,
            crate,
            creature
        }

        public MainWindow()
        {
            H2Ek_install_path = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Halo 2", "tools_directory", H2Ek_install_path).ToString();
            InitializeComponent();
#if !DEBUG
            string our_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string latest_version;
            using (var wc = new System.Net.WebClient())
                latest_version = wc.DownloadString("https://ci.appveyor.com/api/projects/num0005/h2-toolkit-launcher/artifacts/version");
            if (latest_version != our_version)
                MessageBox.Show("Outdated Version! Latest version is: " + latest_version + " You are using: " + our_version);
#endif
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

        private void HandleClickCompile(object sender, RoutedEventArgs e)
        {
            string level_path = compile_level_path.Text;
            if (File.Exists(level_path))
            {
                light_quality light_level = (light_quality)light_quality_level.SelectedIndex;
                new Thread(delegate ()
                {
                    CompileLevel(level_path.ToLower(), light_level);
                }).Start();
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void CompileLevel(string level_path, light_quality lightQuality)
        {
            if (levelCompileType.HasFlag(level_compile_type.compile))
            {
                string command = (level_path .Contains(".ass") ? "structure-new-from-ass" : "structure-new-from-jms");
                var proc = Start(H2Ek_install_path + "h2tool.exe", command + " \"" + level_path + "\" yes");
                proc.WaitForExit(-1);
            }
            if (levelCompileType.HasFlag(level_compile_type.light))
            {
                level_path = level_path.Replace(".ass", "");
                level_path = level_path.Replace(".jms", "");
                level_path = level_path.Replace("\\data\\", "\\tags\\");
                level_path = level_path.Replace("\\structure\\", "\\");
                Start(H2Ek_install_path + "h2tool.exe", "lightmaps \"" + level_path + "\" " 
                    + System.IO.Path.GetFileNameWithoutExtension(level_path) + " " + lightQuality);
                System.IO.Path.GetFileNameWithoutExtension(level_path);
            }
        }

        private void CompileText(object sender, RoutedEventArgs e)
        {
            string text_path = compile_text_path.Text;
            if (File.Exists(text_path))
            {
                string path = new FileInfo(text_path).Directory.FullName;
                Start(H2Ek_install_path + "h2tool.exe", "new-strings \"" + path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void CompileImage(object sender, RoutedEventArgs e)
        {
            string image_path = compile_image_path.Text;
            if (File.Exists(image_path))
            {
                string path = new FileInfo(image_path).Directory.FullName;
                Start(H2Ek_install_path + "h2tool.exe", "bitmaps \"" + path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void PackageLevel(object sender, RoutedEventArgs e)
        {
            string level_path = package_level_path.Text;
            if (File.Exists(level_path))
            {
                level_path = level_path.Replace(".scenario", "");
                Start(H2Ek_install_path + "h2tool.exe", "build-cache-file \"" + level_path + "\"");
            }
            else
            {
                MessageBox.Show("Error: No such file!");
            }
        }

        private void CompileOnly_Checked(object sender, RoutedEventArgs e)
        {
            levelCompileType = level_compile_type.compile;
        }

        private void LightOnly_Checked(object sender, RoutedEventArgs e)
        {
            levelCompileType = level_compile_type.light;
        }

        private void CompileAndLight_Checked(object sender, RoutedEventArgs e)
        {
            levelCompileType = level_compile_type.compile | level_compile_type.light;
        }

        private void browse_level_compile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Uncompiled level";
            dlg.Filter = "Uncompiled map geometry|*.ASS;*.JMS";
            if (dlg.ShowDialog() == true)
            {
                compile_level_path.Text = dlg.FileName;
            }
        }

        private void Browse_text_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select unicode encoded .txt file to compile.";
            dlg.Filter = "Unicode encoded .txt files|*.txt";
            if (dlg.ShowDialog() == true)
            {
                compile_text_path.Text = dlg.FileName;
            }
        }

        private void browse_bitmap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Image File";
            dlg.Filter = "Supported image files|*.tif;*.tga;*.jpg;*.bmp";
            if (dlg.ShowDialog() == true)
            {
                compile_image_path.Text = dlg.FileName;
            }
        }

        private void browse_package_level_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Scenario";
            dlg.Filter = "Unpackaged Map|*.scenario";
            if (dlg.ShowDialog() == true)
            {
                package_level_path.Text = dlg.FileName;
            }
        }

        private void run_cmd_Click(object sender, RoutedEventArgs e)
        {
            Start("cmd", "/K \"cd /d \"" + H2Ek_install_path + "\"");
        }

        private void custom_h2tool_cmd_Click(object sender, RoutedEventArgs e)
        {
            Custom_Command.Visibility = Visibility.Visible;
        }

        private void custom_cancel_Click(object sender, RoutedEventArgs e)
        {
            Custom_Command.Visibility = Visibility.Collapsed;
            custom_command_text.Text = "";
        }

        private void custom_run_Click(object sender, RoutedEventArgs e)
        {
            Custom_Command.Visibility = Visibility.Collapsed;
            Start(H2Ek_install_path + "h2tool.exe", custom_command_text.Text);
            custom_command_text.Text = "";
        }

        private void compile_model_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Annoy num0005#8646 on discord if you want this to work.");
        }

        private void browse_model_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Annoy num0005#8646 on discord if you want this to work.");
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
