using Get_TLE.Models;
using Get_TLE.Utilities;
using Get_TLE.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Get_TLE.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SatelliteMapping> Mappings { get; set; } = new ObservableCollection<SatelliteMapping>();

        private SatelliteMapping _selectedMapping;
        public SatelliteMapping SelectedMapping
        {
            get => _selectedMapping;
            set { _selectedMapping = value; OnPropertyChanged(nameof(SelectedMapping)); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand FetchTleCommand { get; }

        private readonly string _mappingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "satellite_mappings.json");

        public MainViewModel()
        {
            AddCommand = new RelayCommand(_ => AddMapping());
            RemoveCommand = new RelayCommand(_ => RemoveMapping(), _ => SelectedMapping != null);
            FetchTleCommand = new RelayCommand(async _ => await FetchAndProcessTle());

            LoadMappings();
        }

        private void LoadMappings()
        {
            if (File.Exists(_mappingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_mappingsPath);
                    var list = JsonSerializer.Deserialize<ObservableCollection<SatelliteMapping>>(json);
                    if (list != null)
                    {
                        Mappings.Clear();
                        foreach (var item in list)
                        {
                            Mappings.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}");
                }
            }
        }

        private void SaveMappings()
        {
            try
            {
                var json = JsonSerializer.Serialize(Mappings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_mappingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void AddMapping()
        {
            var dlg = new AddMappinngWindow();
            if (dlg.ShowDialog() == true && dlg.Tag is SatelliteMapping mapping)
            {
                Mappings.Add(mapping);
                SaveMappings();
            }
        }

        private void RemoveMapping()
        {
            if (SelectedMapping != null)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить выбранный спутник?",
                    "Подтверждение удаления", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Mappings.Remove(SelectedMapping);
                    SaveMappings();
                }
            }
        }

        public async Task FetchAndProcessTle()
        {
            if (Mappings.Count == 0)
            {
                MessageBox.Show("Нет добавленных спутников для получения TLE.");
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    // Авторизация на Space-Track
                    var loginData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("identity", Properties.Settings.Default.SpacetrackLogin),
                        new KeyValuePair<string, string>("password", Properties.Settings.Default.SpacetrackPassword)
                    });

                    var response = await client.PostAsync("https://www.space-track.org/ajaxauth/login", loginData);

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Ошибка авторизации на Space-Track. Проверьте логин и пароль.");
                        return;
                    }

                    // Формируем список уникальных NORAD ID для запроса
                    var uniqueNoradIds = Mappings.Select(m => m.NoradId).Distinct();
                    var noradIds = string.Join(",", uniqueNoradIds);
                    var tleUrl = $"https://www.space-track.org/basicspacedata/query/class/tle_latest/NORAD_CAT_ID/{noradIds}/format/tle";

                    var tleResponse = await client.GetAsync(tleUrl);
                    tleResponse.EnsureSuccessStatusCode();

                    var tleRaw = await tleResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(tleRaw))
                    {
                        MessageBox.Show("Не получены данные TLE от сервера.");
                        return;
                    }

                    var lines = tleRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var sb = new StringBuilder();

                    // Создаем словарь для быстрого доступа к TLE по NORAD ID
                    var tleDict = new Dictionary<int, (string line1, string line2)>();

                    // Обрабатываем TLE построчно и заполняем словарь
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        var line1 = lines[i];
                        var line2 = lines[i + 1];

                        if (line1.StartsWith("1 ") && line2.StartsWith("2 "))
                        {
                            if (int.TryParse(line1.Substring(2, 5), out int id))
                            {
                                tleDict[id] = (line1, line2);
                            }
                            i++; // Пропускаем вторую строку
                        }
                    }

                    // Теперь обрабатываем все mapping'и
                    foreach (var map in Mappings)
                    {
                        if (tleDict.TryGetValue(map.NoradId, out var tle))
                        {
                            if (map.IsFake && map.FakeId.HasValue)
                            {
                                // Обрабатываем фейковый спутник
                                sb.AppendLine(map.FakeName);
                                sb.Append(ModifyTleForFake(tle.line1, tle.line2, map.FakeId.Value));
                            }
                            else
                            {
                                // Обрабатываем обычный спутник - только имя, без NORAD ID
                                sb.AppendLine(map.DisplayName);
                                sb.AppendLine(tle.line1);
                                sb.AppendLine(tle.line2);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Не найдены TLE данные для спутника с NORAD ID: {map.NoradId}");
                        }
                    }

                    // Сохраняем результат
                    var folder = string.IsNullOrWhiteSpace(Properties.Settings.Default.SaveFolderPath)
                        ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TLE")
                        : Properties.Settings.Default.SaveFolderPath;

                    Directory.CreateDirectory(folder);

                    var template = Properties.Settings.Default.DefaultFileName;
                    var fileName = template.Replace("{yyyyMMdd_HHmmss}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    var fullPath = Path.Combine(folder, fileName);

                    File.WriteAllText(fullPath, sb.ToString());

                    MessageBox.Show($"TLE данные успешно сохранены в файл:\n{fullPath}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении TLE: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ModifyTleForFake(string line1, string line2, int newId)
        {
            // Заменяем NORAD ID в обеих строках TLE
            line1 = line1.Substring(0, 2) + newId.ToString("D5") + line1.Substring(7);
            line2 = line2.Substring(0, 2) + newId.ToString("D5") + line2.Substring(7);

            // Исправляем контрольные суммы
            line1 = FixChecksum(line1);
            line2 = FixChecksum(line2);

            return line1 + "\r\n" + line2 + "\r\n";
        }

        private string FixChecksum(string line)
        {
            if (line.Length < 69)
                return line;

            int checksum = 0;
            for (int i = 0; i < 68; i++)
            {
                char c = line[i];
                if (char.IsDigit(c))
                    checksum += (int)char.GetNumericValue(c);
                else if (c == '-')
                    checksum += 1;
            }
            checksum %= 10;
            return line.Substring(0, 68) + checksum.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public static class SatelliteMappingExtensions
    {
        public static string DisplayText(this SatelliteMapping map)
        {
            if (map.IsFake && map.FakeId.HasValue)
            {
                return $"{map.NoradId} → {map.FakeId} - {map.FakeName}";
            }
            return $"{map.NoradId} - {map.DisplayName}";
        }
    }

    public class MappingToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SatelliteMapping map)
            {
                return map.DisplayText();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}