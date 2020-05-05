using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Net;
using Microsoft.VisualBasic.FileIO;
using static System.Environment;
using static System.Diagnostics.Process;
using Microsoft.WindowsAPICodePack.Dialogs;
using H2CodezLauncher.Properties;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Threading;
using PETools;
using System.Text.RegularExpressions;

namespace Halo2CodezLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string H2Ek_install_path = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Microsoft Games\\Halo 2 Map Editor\\";
        private string Halo_install_path = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Microsoft Games\\Halo 2\\";
        private string Launcher_Directory = AppDomain.CurrentDomain.BaseDirectory;
        private string Guerilla_key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Halo 2";
        private string Tools_Directory = "tools_directory";
        bool startup_finished = false;

        [Flags]
        enum level_compile_type : Byte
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
            draft_super,
            low,
            medium,
            high,
            super,
            custom
        }

        [Flags]
        enum model_compile : Byte
        {
            none = 0,
            collision = 2,
            physics = 4,
            render = 8,
            animations = 16,
            obj = 32,
        }
        model_compile model_compile_type;

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

        enum patch_status
        {
            bad_version,
            unpatched,
            patched
        }

        [Flags]
        enum file_list : Byte
        {
            none = 0,
            tool = 2,
            sapien = 4,
            guerilla = 8
        }

        enum tool_type
        {
            tool,
            sapien,
            guerilla,
            daeconverter
        }

        void RunProcess(ProcessStartInfo info, bool wait = false)
        {
            try
            {
                Process proc = Start(info);
                if (wait)
                    proc.WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                /*
                https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes
                */
                int ErrorCode = ex.NativeErrorCode;
                if (ErrorCode == 2)
                {
                    CustomOkDialogWPF.MsgBoxOk Cant_Find_File = new CustomOkDialogWPF.MsgBoxOk(string.Format(H2CodezLauncher.Properties.Resources.Cant_Find_File, info.FileName), H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                    Cant_Find_File.ShowDialog();
                }
                else if (ErrorCode == 1223)
                {
                    CustomOkDialogWPF.MsgBoxOk Launch_Cancelled = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Launch_Cancelled, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                    Launch_Cancelled.ShowDialog();
                }
                else
                {
                    throw ex;
                }
            }
        }

        string GetToolExeName(tool_type type)
        {
            string name;
            switch (type)
            {
                case tool_type.tool:
                    name = "h2tool";
                    break;
                case tool_type.sapien:
                    name = "h2sapien";
                    break;
                case tool_type.guerilla:
                    name = "h2guerilla";
                    break;
                case tool_type.daeconverter:
                    name = "daeconverter";
                    break;
                default:
                    name = "";
                    break;
            }
            if (type != tool_type.guerilla && type != tool_type.daeconverter && Settings.Default.large_address_support)
                name += ".large_address";
            return name + ".exe";
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is UnauthorizedAccessException)
            {
                e.Handled = true;
                RelaunchAsAdmin("");
            }

        }

        void updateLaucherCheck(WebClient wc)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string our_version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            string latest_version = wc.DownloadString(Settings.Default.version_url);
            if (latest_version != our_version && !Settings.Default.ignore_updates)
            {
                CustomYesNoDialogWPF.MsgBoxYesNo Update_Check = new CustomYesNoDialogWPF.MsgBoxYesNo(string.Format(H2CodezLauncher.Properties.Resources.Update_Check, latest_version, our_version), H2CodezLauncher.Properties.Resources.Outdated_Version, H2CodezLauncher.Properties.Resources.Yes, H2CodezLauncher.Properties.Resources.No);
                if ((bool)Update_Check.ShowDialog())
                {
                    string launcher_filename = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
                    AllowReadWriteFile(launcher_filename);

                    wc.DownloadFile(Settings.Default.launcher_update_url, "H2CodezLauncher.exe.new");
                    ForceMove(launcher_filename, "H2CodezLauncher.exe.old");
                    ForceMove("H2CodezLauncher.exe.new", "H2CodezLauncher.exe");
                    AllowReadWriteFile("H2CodezLauncher.exe");
                    AllowReadWriteFile("H2CodezLauncher.exe.old");
                    Start("H2CodezLauncher.exe", "--update");
                    Exit(0);
                }
            }
        }

        void patch_exes_for_large_address_support()
        {
            List<string> files_to_patch = new List<string> { "h2tool", "h2sapien" };
            PETool pe = new PETool();

            foreach (string file in files_to_patch)
            {
                if (!File.Exists(H2Ek_install_path + file + ".large_address.exe") && File.Exists(H2Ek_install_path + file + ".exe"))
                {
                    pe.Read(H2Ek_install_path + file + ".exe");
                    pe.fileHeader.Characteristics |= PECharacteristics.IMAGE_FILE_LARGE_ADDRESS_AWARE;
                    pe.UpdateHeader();
                    pe.WriteFile(H2Ek_install_path + file + ".large_address.exe");
                }
            }
        }

        void repair_registry(bool force_repair, bool use_launcher_path)
        {
            string H2Tool_Path = AppDomain.CurrentDomain.BaseDirectory;
            if (Settings.Default.portable_install != true || force_repair is true)
            {
                if (Registry.GetValue(Guerilla_key, Tools_Directory, null) is null || force_repair is true)
                {
                    RegistryKey Guerilla_Tag_key = RegistryKey
                        .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                        .CreateSubKey("SOFTWARE\\Microsoft\\Halo 2");

                    CustomOkDialogWPF.MsgBoxOk Select_H2Tool = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Select_H2Tool, H2CodezLauncher.Properties.Resources.Repair_Registry, H2CodezLauncher.Properties.Resources.Ok);
                    Select_H2Tool.ShowDialog();

                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_H2Tool;
                    dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_H2Tool;

                    if (dlg.ShowDialog() == true)
                    {
                        H2Tool_Path = dlg.FileName;
                    }
                    else
                    {
                        CustomOkDialogWPF.MsgBoxOk Registry_Failed = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Registry_Failed, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                        Registry_Failed.ShowDialog();
                        use_launcher_path = true;
                    }

                    H2Ek_install_path = new FileInfo(H2Tool_Path).Directory.FullName;

                    if (use_launcher_path != true)
                    {
                        if (Registry.GetValue(Guerilla_key, Tools_Directory, null) is null || force_repair == true)
                        {
                            Guerilla_Tag_key.SetValue("tools_directory", H2Ek_install_path + "\\");
                            Guerilla_Tag_key.Close();
                            CustomOkDialogWPF.MsgBoxOk Registry_Passed = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Registry_Passed, H2CodezLauncher.Properties.Resources.Repair_Registry, H2CodezLauncher.Properties.Resources.Ok);
                            Registry_Passed.ShowDialog();
                        }
                        force_repair = false;
                    }

                    if (Settings.Default.portable_install == true)
                    {
                        Settings.Default.portable_install = false;
                        Settings.Default.Save();
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            portable_install_enabled.IsChecked = Settings.Default.portable_install;
                        }));
                    }
                }
            }
        }

        void set_language()
        {
            /*
            https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c?redirectedfrom=MSDN 
            */

            int language = Settings.Default.language;
            string language_code;

            if (language == 0)
            {
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InstalledUICulture;
                language_code = ci.ToString();
            }
            else if (language == 1)
            {
                language_code = "en";
            }
            else if (language == 2)
            {
                language_code = "es";
            }
            else if (language == 3)
            {
                language_code = "fr";
            }
            else
            {
                language_code = "en";
            }

            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(language_code);
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(language_code);
        }

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            Settings.Default.Upgrade();
            Settings.Default.Save();
            set_language();

            InitializeComponent();
            large_addr_enabled.IsChecked = Settings.Default.large_address_support;
            ignore_updates_enabled.IsChecked = Settings.Default.ignore_updates;
            portable_install_enabled.IsChecked = Settings.Default.portable_install;
            select_language.SelectedIndex = Settings.Default.language;

            // Delete any left over update files.
            try
            {
                File.Delete("H2CodezLauncher.exe.old");
            } catch // but don't crash if that fails.
            {

            }

            var wc = new WebClient();

            Thread startup = new Thread(delegate ()
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    if (Settings.Default.portable_install != true)
                    {
                        if (Registry.GetValue(Guerilla_key, Tools_Directory, null) is null)
                        {
                            CustomYesNoDialogWPF.MsgBoxYesNo Ask_Portable = new CustomYesNoDialogWPF.MsgBoxYesNo(H2CodezLauncher.Properties.Resources.Ask_Portable, H2CodezLauncher.Properties.Resources.Ask_Portable_Header, H2CodezLauncher.Properties.Resources.Yes, H2CodezLauncher.Properties.Resources.No);
                            if ((bool)Ask_Portable.ShowDialog())
                            {
                                CustomOkDialogWPF.MsgBoxOk Portable_True = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Portable_True, H2CodezLauncher.Properties.Resources.Portable_True_Header, H2CodezLauncher.Properties.Resources.Ok);
                                Portable_True.ShowDialog();
                                Settings.Default.portable_install = true;
                                Settings.Default.Save();
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    portable_install_enabled.IsChecked = Settings.Default.portable_install;
                                }));
                            }
                            else
                            {
                                repair_registry(false, false);
                            }
                        }
                    }

                    if (Registry.GetValue(Guerilla_key, Tools_Directory, null) is null && Settings.Default.portable_install == true)
                    {
                        H2Ek_install_path = new FileInfo(Launcher_Directory).Directory.FullName + "\\";
                    }
                    else
                    {
                        H2Ek_install_path = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Halo 2", "tools_directory", H2Ek_install_path).ToString();
                    }

                    updateLaucherCheck(wc);
                    file_list files_to_patch = file_list.none;
                    if (!check_files(ref files_to_patch))
                    {
                        CustomOkDialogWPF.MsgBoxOk Unsupported_Toolkit = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Unsupported_Toolkit, H2CodezLauncher.Properties.Resources.Unsupported_Toolkit_Header, H2CodezLauncher.Properties.Resources.Ok);
                        Unsupported_Toolkit.ShowDialog();
                        return;
                    }

                    if (!File.Exists(H2Ek_install_path + "H2Codez.dll") || files_to_patch != file_list.none)
                    {
                        CustomYesNoDialogWPF.MsgBoxYesNo H2Codez_Not_Found = new CustomYesNoDialogWPF.MsgBoxYesNo(H2CodezLauncher.Properties.Resources.H2Codez_Not_Found, H2CodezLauncher.Properties.Resources.H2Codez_Not_Found_Header, H2CodezLauncher.Properties.Resources.Yes, H2CodezLauncher.Properties.Resources.No);
                        if ((bool)H2Codez_Not_Found.ShowDialog() == false) return;

                        AllowReadWriteDir(H2Ek_install_path, true); // wipe permissions for install path and let all users access it
                        ApplyPatches(files_to_patch, wc);
                        patch_exes_for_large_address_support();
                        wc.DownloadFile(Settings.Default.h2codez_update_url, H2Ek_install_path + "H2Codez.dll");
                        AllowReadWriteFile(H2Ek_install_path + "H2Codez.dll");
                        CustomOkDialogWPF.MsgBoxOk H2Codez_Install_Passed = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.H2Codez_Install_Passed, H2CodezLauncher.Properties.Resources.H2Codez_Install_Header, H2CodezLauncher.Properties.Resources.Ok);
                        H2Codez_Install_Passed.ShowDialog();
                        return;
                    }

                    patch_exes_for_large_address_support();
                    string h2codez_latest_hash = wc.DownloadString(Settings.Default.h2codez_lastest_hash);
                    string our_h2codes_hash = CalculateMD5(H2Ek_install_path + "H2Codez.dll");
                    if (our_h2codes_hash != h2codez_latest_hash.ToLower() && !Settings.Default.ignore_updates)
                    {
                        CustomYesNoDialogWPF.MsgBoxYesNo H2Codez_Update = new CustomYesNoDialogWPF.MsgBoxYesNo(H2CodezLauncher.Properties.Resources.H2Codez_Update, H2CodezLauncher.Properties.Resources.H2Codez_Update_Header, H2CodezLauncher.Properties.Resources.Yes, H2CodezLauncher.Properties.Resources.No);
                        if ((bool)H2Codez_Update.ShowDialog())
                        {
                            AllowReadWriteDir(H2Ek_install_path, true); // wipe permissions for install path and let all users access it
                            AllowReadWriteFile(H2Ek_install_path + "H2Codez.dll");
                            wc.DownloadFile(Settings.Default.h2codez_update_url, H2Ek_install_path + "H2Codez.dll");
                            CustomOkDialogWPF.MsgBoxOk H2Codez_Update_Passed = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.H2Codez_Update_Passed, H2CodezLauncher.Properties.Resources.H2Codez_Update_Passed_Header, H2CodezLauncher.Properties.Resources.Ok);
                            H2Codez_Update_Passed.ShowDialog();
                        }
                    }

                    startup_finished = true;
                }
                catch (UnauthorizedAccessException)
                {
                    RelaunchAsAdmin("");
                }
                catch (WebException ex) when (ex.InnerException is IOException)
                {
                    CustomOkDialogWPF.MsgBoxOk H2Codez_In_Use = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.H2Codez_In_Use, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                    H2Codez_In_Use.ShowDialog();
                }
                catch (WebException ex) when (ex.InnerException is UnauthorizedAccessException)
                {
                    RelaunchAsAdmin("");
                }
                catch (WebException)
                {
                    CustomOkDialogWPF.MsgBoxOk H2Codez_No_Net = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.H2Codez_No_Net, H2CodezLauncher.Properties.Resources.H2Codez_No_Net_Header, H2CodezLauncher.Properties.Resources.Ok);
                    H2Codez_No_Net.ShowDialog();
                }
            });
            startup.SetApartmentState(ApartmentState.STA);
            startup.Start();
        }

        private string get_default_path(string textbox_path, string compile_type)
        {
            string asset_folder = "data\\";
            if (compile_type == "package_level_path")
            {
                asset_folder = "tags\\";
            }

            string path = H2Ek_install_path + asset_folder;
            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                string documents_halo2_asset = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Halo 2\\" + asset_folder;

                bool data_path_exists = System.IO.Directory.Exists(documents_halo2_asset);

                if (!data_path_exists)
                    System.IO.Directory.CreateDirectory(documents_halo2_asset);

                path = documents_halo2_asset;
            }

            if (!string.IsNullOrWhiteSpace(textbox_path))
            {
                path = System.IO.Path.GetDirectoryName(textbox_path) + "\\";
                if (compile_type == "compile_model_path")
                {
                    path = System.IO.Path.GetFullPath(textbox_path) + "\\";
                }
            }

            return path;
        }

        private void ForceMove(string sourceFilename, string destinationFilename)
        {
            if (!File.Exists(sourceFilename))
                return;
            if (File.Exists(destinationFilename))
            {
                System.IO.File.Delete(destinationFilename);
            }

            System.IO.File.Move(sourceFilename, destinationFilename);
        }

        private void RelaunchAsAdmin(string arguments)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = CurrentDirectory;
            proc.FileName = "H2CodezLauncher.exe";
            proc.Verb = "runas";
            proc.Arguments = arguments;

            try
            {
                Process.Start(proc);
            }
            catch
            {
            }
            Exit(0);
        }

        private void AllowReadWriteFile(string filename)
        {
            FileSecurity sec = File.GetAccessControl(filename);

            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(
                    everyone,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));

            AuthorizationRuleCollection rules = sec.GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount));
            foreach (FileSystemAccessRule rule in rules)
                if (rule.IdentityReference.Value != "Everyone")
                    sec.RemoveAccessRuleSpecific(rule);

            File.SetAccessControl(filename, sec);
        }

        private void AllowReadWriteDir(string filename, bool recursive = true, int depth_limit = 10, int depth_count = 0)
        {
            if (depth_count > depth_limit)
                return;
            DirectorySecurity sec = Directory.GetAccessControl(filename);

            // stop new inheriate rules and remove existing ones
            sec.SetAccessRuleProtection(true, false);

            // setup new access rules letting everyone access the directory
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(
                    everyone,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

            AuthorizationRuleCollection rules = sec.GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount));
            foreach (FileSystemAccessRule rule in rules)
                if (rule.IdentityReference.Value != "Everyone")
                    sec.RemoveAccessRuleSpecific(rule);

            Directory.SetAccessControl(filename, sec);

            if (!recursive)
                return;

            string[] files;
            string[] directories;

            files = Directory.GetFiles(filename);
            foreach (string file in files)
            {
                AllowReadWriteFile(file);
            }

            directories = Directory.GetDirectories(filename);
            foreach (string directory in directories)
            {
                AllowReadWriteDir(directory, true, depth_limit, depth_count++);
            }
        }

        private void RunHalo2Sapien(object sender, RoutedEventArgs e)
        {
            var process = new ProcessStartInfo();
            process.WorkingDirectory = H2Ek_install_path;
            process.FileName = GetToolExeName(tool_type.sapien);
            RunProcess(process);
        }
        private void RunHalo2Guerilla(object sender, RoutedEventArgs e)
        {
            var process = new ProcessStartInfo();
            process.WorkingDirectory = H2Ek_install_path;
            process.FileName = GetToolExeName(tool_type.guerilla);
            RunProcess(process);
        }
        private void RunHalo2(object sender, RoutedEventArgs e)
        {
            var process = new ProcessStartInfo();
            process.WorkingDirectory = Halo_install_path;
            process.FileName = "halo2.exe";
            RunProcess(process);
        }

        static string CalculateMD5(string filename)
        {
            if (!File.Exists(filename))
                return "";
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private bool check_files(ref file_list files_to_patch)
        {
            string h2tool = CalculateMD5(H2Ek_install_path + "h2tool.exe");
            if (h2tool == "dc221ca8c917a1975d6b3dd035d2f862")
                files_to_patch |= file_list.tool;
            else if (h2tool != "f81c24da93ce8d114caa8ba0a21c7a63")
                return false;

            string h2sapien = CalculateMD5(H2Ek_install_path + "h2sapien.exe");
            if (h2sapien == "d86c488b7c8f64b86f90c732af01bf50")
                files_to_patch |= file_list.sapien;
            else if (h2sapien != "975c0d0ad45c1687d11d7d3fdfb778b8")
                return false;

            string h2guerilla = CalculateMD5(H2Ek_install_path + "h2guerilla.exe");
            if (h2guerilla == "ce3803cc90e260b3dc59854d89b3ea88")
                files_to_patch |= file_list.guerilla;
            else if (h2guerilla != "55b09d5a6c8ecd86988a5c0f4d59d7ea")
                return false;

            return true;
        }

        private void ApplyPatches(file_list files_to_patch, WebClient wc)
        {
            Directory.CreateDirectory(H2Ek_install_path + "backup");
            ForceMove(H2Ek_install_path + "Halo_2_Map_Editor_Launcher.exe", H2Ek_install_path + "backup\\Halo_2_Map_Editor_Launcher.exe");
            if (files_to_patch.HasFlag(file_list.tool))
                patch_file("h2tool.exe", wc);
            if (files_to_patch.HasFlag(file_list.guerilla))
                patch_file("h2guerilla.exe", wc);
            if (files_to_patch.HasFlag(file_list.sapien))
                patch_file("h2sapien.exe", wc);
        }

        private void patch_file(string name, WebClient wc)
        {
            byte[] patch_data = wc.DownloadData(Settings.Default.patch_fetch_url + name + ".patch");
            using (FileStream unpatched_file = new FileStream(H2Ek_install_path + name, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream patched_file = new FileStream(H2Ek_install_path + name + ".patched", FileMode.Create))
                BsDiff.BinaryPatchUtility.Apply(unpatched_file, () => new MemoryStream(patch_data), patched_file);
            ForceMove(H2Ek_install_path + name, H2Ek_install_path + "backup\\" + name);
            ForceMove(H2Ek_install_path + name + ".patched", H2Ek_install_path + name);
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

        private void RepairRegistry(object sender, RoutedEventArgs e)
        {
            Thread repair = new Thread(delegate ()
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    repair_registry(true, false);
                }
                catch (UnauthorizedAccessException)
                {
                    RelaunchAsAdmin("");
                }
            });
            repair.SetApartmentState(ApartmentState.STA);
            repair.Start();
        }

        private void SetPermissions(object sender, RoutedEventArgs e)
        {
            Thread set_permissions = new Thread(delegate ()
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    AllowReadWriteDir(H2Ek_install_path, true); // wipe permissions for install path and let all users access it
                    CustomOkDialogWPF.MsgBoxOk H2Codez_Set_Permissions = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.H2Codez_Set_Permissions, H2CodezLauncher.Properties.Resources.Repair_Registry, H2CodezLauncher.Properties.Resources.Ok);
                    H2Codez_Set_Permissions.ShowDialog();
                }
                catch (UnauthorizedAccessException)
                {
                    RelaunchAsAdmin("");
                }
            });
            set_permissions.SetApartmentState(ApartmentState.STA);
            set_permissions.Start();
        }

        private void HandleClickCompile(object sender, RoutedEventArgs e)
        {
            string level_path = compile_level_path.Text;
            string instance_text = instance_value.Text;
            if (File.Exists(level_path))
            {
                light_quality light_level = (light_quality)light_quality_level.SelectedIndex;
                Thread compile_level = new Thread(delegate ()
                {
                    CompileLevel(level_path.ToLower(), light_level, instance_text);
                });
                compile_level.SetApartmentState(ApartmentState.STA);
                compile_level.Start();
            }
            else
            {
                CustomOkDialogWPF.MsgBoxOk File_Not_Found = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.File_Not_Found, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                File_Not_Found.ShowDialog();
            }
        }

        private void CompileLevel(string level_path, light_quality lightQuality, string instance_text)
        {
            if (levelCompileType.HasFlag(level_compile_type.compile))
            {
                string command = (level_path.Contains(".ass") ? "structure-new-from-ass" : "structure-new-from-jms");
                var process = new ProcessStartInfo();
                process.WorkingDirectory = H2Ek_install_path;
                process.FileName = GetToolExeName(tool_type.tool);
                process.Arguments = command + " \"" + level_path + "\" yes";
                process.Arguments += " pause_after_run";
                RunProcess(process, true);
            }
            if (levelCompileType.HasFlag(level_compile_type.light))
            {
                Int32 instance_count;
                if (Int32.TryParse(instance_text, out instance_count))
                {
                    level_path = level_path.Replace(".ass", "");
                    level_path = level_path.Replace(".jms", "");
                    level_path = level_path.Replace("\\data\\", "\\tags\\");
                    level_path = level_path.Replace("\\structure\\", "\\");
                    string scenario_path = new FileInfo(level_path).Directory.FullName;

                    string common_args = "\"" + scenario_path + "\\" + System.IO.Path.GetFileName(scenario_path) + "\" " + "\"" + System.IO.Path.GetFileName(level_path) + "\" " + lightQuality;

                    var process = new ProcessStartInfo();
                    process.WorkingDirectory = H2Ek_install_path;
                    process.FileName = GetToolExeName(tool_type.tool);

                    if (instance_count >= 2)
                    {
                        process.Arguments = "lightmaps-local-multi-process " + common_args + " " + instance_count; 
                    } else
                    {
                        process.Arguments = "lightmaps " + common_args;
                    }
                    process.Arguments += " pause_after_run";
                    RunProcess(process);
                } else
                {
                    CustomOkDialogWPF.MsgBoxOk Invalid_Instance_Count = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Invalid_Instance_Count, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                    Invalid_Instance_Count.ShowDialog();
                }
            }
        }

        private void CompileText(object sender, RoutedEventArgs e)
        {
            string text_path = compile_text_path.Text;
            if (File.Exists(text_path))
            {
                string path = new FileInfo(text_path).Directory.FullName;
                var process = new ProcessStartInfo();
                process.WorkingDirectory = H2Ek_install_path;
                process.FileName = GetToolExeName(tool_type.tool);
                process.Arguments = "new-strings \"" + path + "\"";
                process.Arguments += " pause_after_run";
                RunProcess(process);
            }
            else
            {
                CustomOkDialogWPF.MsgBoxOk File_Not_Found = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.File_Not_Found, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                File_Not_Found.ShowDialog();
            }
        }

        class BitmapCompile
        {
            public static List<string> bitmapType = new List<string>()
            {
                "2d",
                "3d",
                "cubemaps",
                "sprites",
                "inteface"
            };
        }

        private void CompileImage(object sender, RoutedEventArgs e)
        {
            string image_path = compile_image_path.Text;
            if (File.Exists(image_path))
            {
                string listEntry = BitmapCompile.bitmapType[bitmap_compile_type.SelectedIndex];
                string path = new FileInfo(image_path).Directory.FullName;
                var process = new ProcessStartInfo();
                process.WorkingDirectory = H2Ek_install_path;
                process.FileName = GetToolExeName(tool_type.tool);
                process.Arguments = "bitmaps-with-type \"" + path + "\" " + listEntry;
                process.Arguments += " pause_after_run";
                RunProcess(process);
            }
            else
            {
                CustomOkDialogWPF.MsgBoxOk File_Not_Found = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.File_Not_Found, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                File_Not_Found.ShowDialog();
            }
        }

        private void PackageLevel(object sender, RoutedEventArgs e)
        {
            string level_path = package_level_path.Text;
            if (File.Exists(level_path))
            {
                bool copy_map = (bool)this.copy_map.IsChecked;
                bool remove_shared_tags = (bool)shared_tag_removal.IsChecked;
                bool cut_map = false;
                Thread package_level = new Thread(delegate ()
                {
                    if (Keyboard.IsKeyDown(Key.LeftAlt))
                    {
                        cut_map = true;
                    }
                    var process = new ProcessStartInfo();
                    process.WorkingDirectory = H2Ek_install_path;
                    process.FileName = GetToolExeName(tool_type.tool);
                    process.Arguments = "build-cache-file \"" + level_path.Replace(".scenario", "") + "\" ";
                    process.Arguments += remove_shared_tags ? "shared_tag_removal pause_after_run" : "pause_after_run";
                    RunProcess(process, copy_map);
                    if (!copy_map)
                        return;

                    string map_name = new FileInfo(level_path).Name;
                    map_name = map_name.Replace(".scenario", ".map");

                    string copy_to = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\My Games\\Halo 2\\Maps\\" + map_name;
                    String[] tags_path = Regex.Split(level_path, "tags");
                    string copy_from = tags_path[0] + "Maps\\" + map_name;
                    if (cut_map)
                    {
                        try
                        {
                            File.Delete(copy_to);
                            FileSystem.MoveFile(
                                copy_from, copy_to, UIOption.AllDialogs, UICancelOption.DoNothing);
                        }
                        catch { };
                    }
                    else
                    {
                        try
                        {
                            File.Delete(copy_to);
                            FileSystem.CopyFile(
                                copy_from, copy_to, UIOption.AllDialogs, UICancelOption.DoNothing);
                        }
                        catch { };
                    }
                });
                package_level.SetApartmentState(ApartmentState.STA);
                package_level.Start();
            }
            else
            {
                CustomOkDialogWPF.MsgBoxOk File_Not_Found = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.File_Not_Found, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                File_Not_Found.ShowDialog();
            }
        }

        private void CompileOnly_Checked(object sender, RoutedEventArgs e)
        {
            levelCompileType = level_compile_type.compile;
            light_quality_select_box.IsEnabled = false;
        }

        private void LightOnly_Checked(object sender, RoutedEventArgs e)
        {
            levelCompileType = level_compile_type.light;
            light_quality_select_box.IsEnabled = true;
        }

        private void CompileAndLight_Checked(object sender, RoutedEventArgs e)
        {
            levelCompileType = level_compile_type.compile | level_compile_type.light;
            light_quality_select_box.IsEnabled = true;
        }

        private void browse_level_compile_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "compile_level_path";
            string path = get_default_path(compile_level_path.Text, compile_name);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_Level;
            dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_Level;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == true)
            {
                compile_level_path.Text = dlg.FileName;
            }
        }

        private void Browse_text_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "compile_text_path";
            string path = get_default_path(compile_text_path.Text, compile_name);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_Text;
            dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_Text;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == true)
            {
                compile_text_path.Text = dlg.FileName;
            }
        }

        private void browse_bitmap_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "compile_image_path";
            string path = get_default_path(compile_image_path.Text, compile_name);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_Bitmap;
            dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_Bitmap;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == true)
            {
                compile_image_path.Text = dlg.FileName;
            }
        }

        private void browse_package_level_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "package_level_path";
            string path = get_default_path(package_level_path.Text, compile_name);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_Scenario;
            dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_Scenario;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == true)
            {
                package_level_path.Text = dlg.FileName;
            }
        }

        private void run_cmd_Click(object sender, RoutedEventArgs e)
        {
            var process = new ProcessStartInfo();
            process.FileName = "cmd";
            process.Arguments = "/K \"cd /d \"" + H2Ek_install_path + "\"";
            RunProcess(process);
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
            var process = new ProcessStartInfo();
            process.WorkingDirectory = H2Ek_install_path;
            process.FileName = GetToolExeName(tool_type.tool);
            process.Arguments = custom_command_text.Text;
            process.Arguments += " pause_after_run";
            RunProcess(process);
            custom_command_text.Text = "";
        }

        private void model_compile_collision_Checked(object sender, RoutedEventArgs e)
        {
            model_compile_type = model_compile.collision;
            model_compile_obj_box.IsEnabled = false;
            model_compile_render_box.IsEnabled = false;
        }

        private void model_compile_physics_Checked(object sender, RoutedEventArgs e)
        {
            model_compile_type = model_compile.physics;
            model_compile_obj_box.IsEnabled = false;
            model_compile_render_box.IsEnabled = false;
        }

        private void model_compile_animations_Checked(object sender, RoutedEventArgs e)
        {
            model_compile_type = model_compile.animations;
            model_compile_obj_box.IsEnabled = false;
            model_compile_render_box.IsEnabled = false;
        }

        private void model_compile_obj_Checked(object sender, RoutedEventArgs e)
        {
            model_compile_type = model_compile.obj;
            model_compile_obj_box.IsEnabled = true;
            model_compile_render_box.IsEnabled = false;
        }

        private void model_compile_all_Checked(object sender, RoutedEventArgs e)
        {
            model_compile_type = model_compile.collision | model_compile.physics | model_compile.obj | model_compile.render | model_compile.animations;
            model_compile_obj_box.IsEnabled = true;
            model_compile_render_box.IsEnabled = true;
        }

        private void model_compile_render_Checked(object sender, RoutedEventArgs e)
        {
            model_compile_type = model_compile.render;
            model_compile_obj_box.IsEnabled = false;
            model_compile_render_box.IsEnabled = true;
        }

        private void compile_model_Click(object sender, RoutedEventArgs e)
        {
            string path = compile_model_path.Text;
            object_type obj = (object_type)model_compile_obj_type.SelectedIndex;
            int render_type = model_compile_render_type.SelectedIndex;
            if (Directory.Exists(path))
            {
                Thread start_model = new Thread(delegate ()
                {
                    var process = new ProcessStartInfo();
                    process.WorkingDirectory = H2Ek_install_path;
                    process.FileName = GetToolExeName(tool_type.tool);
                    if (model_compile_type.HasFlag(model_compile.physics))
                    {
                        process.Arguments = "model-physics \"" + path + "\"";
                        process.Arguments += " pause_after_run";
                        RunProcess(process, true);
                    }
                    if (model_compile_type.HasFlag(model_compile.collision))
                    {
                        process.Arguments = "model-collision \"" + path + "\"";
                        process.Arguments += " pause_after_run";
                        RunProcess(process, true);
                    }
                    if (model_compile_type.HasFlag(model_compile.render))
                    {
                        if (render_type == 0)
                        {
                            process.FileName = GetToolExeName(tool_type.tool);
                            process.Arguments = "model-render \"" + path + "\"";
                            process.Arguments += " pause_after_run";
                            RunProcess(process, true);
                        }
                        else
                        {
                            process.FileName = GetToolExeName(tool_type.daeconverter);
                            process.Arguments = "-compile \"" + path.Replace(H2Ek_install_path, "") + "\""; //DAEConverter adds exe path to the path the user enters.
                            process.Arguments += " pause_after_run";
                            RunProcess(process, true);
                        }

                    }
                    if (model_compile_type.HasFlag(model_compile.animations))
                    {
                        process.Arguments = "append-animations \"" + path + "\"";
                        process.Arguments += " pause_after_run";
                        RunProcess(process, true);
                    }
                    if (model_compile_type.HasFlag(model_compile.obj))
                    {
                        process.Arguments = "model-object \"" + path.Replace(H2Ek_install_path + "data\\", "") + "\\" + Path.GetFileName(path) + "\" " + obj; //Doesn't like a full path?
                        process.Arguments += " pause_after_run";
                        RunProcess(process);
                    }
                });
                start_model.SetApartmentState(ApartmentState.STA);
                start_model.Start();
            }
            else
            {
                CustomOkDialogWPF.MsgBoxOk File_Not_Found = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.File_Not_Found, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                File_Not_Found.ShowDialog();
            }
        }

        private void import_sound_Click(object sender, RoutedEventArgs e)
        {
            /*
            For the future person looking through this. I hail you modder, curious onlooker, 343 employee, whomever. You may see this and be interested in trying it out for yourself.
            Due to the nature of the code in H2Codez any sound files you try to compile must be in documents and should not be given a filename for reasons.
            They also can't be given the path that comes before the tags directory. Good fortune in your hunt.
            */
            if (!string.IsNullOrWhiteSpace(import_sound_path.Text) && string.IsNullOrWhiteSpace(import_lipsync_path.Text))
            {
                string sound_path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(import_sound_path.Text).Replace(H2Ek_install_path + "data\\", ""));
                var process = new ProcessStartInfo();
                process.WorkingDirectory = H2Ek_install_path;
                process.FileName = GetToolExeName(tool_type.tool);
                process.Arguments = "import-sound \"" + sound_path + "\"";
                process.Arguments += " pause_after_run";
                RunProcess(process);
            }

            else if (!string.IsNullOrWhiteSpace(import_sound_path.Text) && !string.IsNullOrWhiteSpace(import_lipsync_path.Text))
            {
                string sound_path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(import_sound_path.Text));
                string ltf_path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(import_lipsync_path.Text), System.IO.Path.GetFileNameWithoutExtension(import_lipsync_path.Text));
                sound_path = sound_path.Replace("\\data\\", "\\tags\\");
                var process = new ProcessStartInfo();
                process.WorkingDirectory = H2Ek_install_path;
                process.FileName = GetToolExeName(tool_type.tool);
                process.Arguments = "import-lipsync \"" + sound_path + "\" " + "\"" + ltf_path + "\"";
                process.Arguments += " pause_after_run";
                RunProcess(process);
            }
            else if (string.IsNullOrWhiteSpace(import_sound_path.Text) && !string.IsNullOrWhiteSpace(import_lipsync_path.Text))
            {
                CustomOkDialogWPF.MsgBoxOk Invalid_Sound_Path = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Invalid_Sound_Path, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                Invalid_Sound_Path.ShowDialog();
            }
            else if (string.IsNullOrWhiteSpace(import_sound_path.Text) && string.IsNullOrWhiteSpace(import_lipsync_path.Text))
            {
                CustomOkDialogWPF.MsgBoxOk Invalid_Sound_LTF_Path = new CustomOkDialogWPF.MsgBoxOk(H2CodezLauncher.Properties.Resources.Invalid_Sound_LTF_Path, H2CodezLauncher.Properties.Resources.H2Codez_In_Use_Header, H2CodezLauncher.Properties.Resources.Ok);
                Invalid_Sound_LTF_Path.ShowDialog();
            }
        }

        private void browse_model_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "compile_model_path";
            string path = get_default_path(compile_model_path.Text, compile_name);

            var dlg = new CommonOpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_Model;
            dlg.IsFolderPicker = true;

            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                compile_model_path.Text = dlg.FileName;
            }
        }

        private void large_addr_enabled_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.large_address_support = (bool)large_addr_enabled.IsChecked;
            Settings.Default.Save();
        }

        private void ignore_updates_enabled_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.ignore_updates = (bool)ignore_updates_enabled.IsChecked;
            Settings.Default.Save();
        }

        private void portable_install_enabled_checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.portable_install = (bool)portable_install_enabled.IsChecked;
            Settings.Default.Save();
        }

        private void language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (startup_finished == true)
            {
                Settings.Default.language = select_language.SelectedIndex;
                Settings.Default.Save();
            }
        }

        private void numbers_only(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = System.Text.RegularExpressions.Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void browse_sound_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "import_sound_path";
            string path = get_default_path(import_sound_path.Text, compile_name);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_Sound;
            dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_Sound;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == true)
            {
                import_sound_path.Text = dlg.FileName;
            }
        }

        private void browse_ltf_Click(object sender, RoutedEventArgs e)
        {
            string compile_name = "import_lipsync_path";
            string path = get_default_path(import_lipsync_path.Text, compile_name);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = H2CodezLauncher.Properties.Resources.File_Dialog_Select_LTF;
            dlg.Filter = H2CodezLauncher.Properties.Resources.File_Dialog_LTF;
            dlg.InitialDirectory = path;

            if (dlg.ShowDialog() == true)
            {
                import_lipsync_path.Text = dlg.FileName;
            }
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
