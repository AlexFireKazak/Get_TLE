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

            NoradId = id;
            SatelliteName = NameTextBox.Text.Trim();
            DialogResult = true;
        }
    }
}
