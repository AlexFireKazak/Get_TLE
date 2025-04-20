using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace Get_TLE.Views
{
    /// <summary>
    /// Логика взаимодействия для CredentialsWindow.xaml
    /// </summary>
    public partial class CredentialsWindow : Window
    {
        public CredentialsWindow()
        {
            InitializeComponent();

            // Загрузка сохранённых настроек
            LoginTextBox.Text = Properties.Settings.Default.SpacetrackLogin;
            PasswordBox.Password = Properties.Settings.Default.SpacetrackPassword;

            // Если путь пустой, устанавливаем по умолчанию в папку TLE рядом с .exe
            string defaultFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TLE");
            FolderTextBox.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.SaveFolderPath)
                ? defaultFolder
                : Properties.Settings.Default.SaveFolderPath;

            FileNameTextBox.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.DefaultFileName)
                ? "tle_{yyyyMMdd_HHmmss}.txt"
                : Properties.Settings.Default.DefaultFileName;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = FolderTextBox.Text;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FolderTextBox.Text = dlg.SelectedPath;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SpacetrackLogin = LoginTextBox.Text.Trim();
            Properties.Settings.Default.SpacetrackPassword = PasswordBox.Password;
            Properties.Settings.Default.SaveFolderPath = FolderTextBox.Text;
            Properties.Settings.Default.DefaultFileName = FileNameTextBox.Text.Trim();
            Properties.Settings.Default.Save();
            DialogResult = true;
        }
    }
}

