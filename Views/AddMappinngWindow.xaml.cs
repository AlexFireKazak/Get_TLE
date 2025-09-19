using Get_TLE.Models;
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
using System.Windows.Shapes;

namespace Get_TLE.Views
{
    /// <summary>
    /// Логика взаимодействия для AddMappinngWindow.xaml
    /// </summary>
    public partial class AddMappinngWindow : Window
    {
        public int NoradId { get; private set; }
        public string SatelliteName { get; private set; }

        public AddMappinngWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(NoradTextBox.Text, out int id))
            {
                MessageBox.Show("NORAD ID должен быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название спутника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var mapping = new SatelliteMapping
                {
                    NoradId = int.Parse(NoradTextBox.Text.Trim()),
                    DisplayName = NameTextBox.Text.Trim(),
                    IsFake = FakeCheckBox.IsChecked == true
                };

                if (mapping.IsFake)
                {
                    if (!int.TryParse(FakeIdTextBox.Text, out int FakeId))
                    {
                        MessageBox.Show("NORAD ID должен быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(FakeNameTextBox.Text))
                    {
                        MessageBox.Show("Введите название спутника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    mapping.FakeId = int.Parse(FakeIdTextBox.Text.Trim());
                    mapping.FakeName = FakeNameTextBox.Text.Trim();
                }

                Tag = mapping;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FakeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FakePanel.Visibility = Visibility.Visible;
        }

        private void FakeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FakePanel.Visibility = Visibility.Collapsed;
        }
    }
}
