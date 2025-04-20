using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32; // для OpenFileDialog, но мы используем Forms
using System.Windows.Forms; // FolderBrowserDialog
using Get_TLE.Models;
using Get_TLE.Views;
using Get_TLE.Utilities;
using System.Collections.Generic;
namespace Get_TLE.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SatelliteMapping> Mappings { get; }
            = new ObservableCollection<SatelliteMapping>();

        private SatelliteMapping _selectedMapping;
        public SatelliteMapping SelectedMapping
        {
            get => _selectedMapping;
            set { _selectedMapping = value; OnPropertyChanged(nameof(SelectedMapping)); }
        }

        private string _saveFolder;
        public string SaveFolderPath
        {
            get => _saveFolder;
            set { _saveFolder = value; OnPropertyChanged(nameof(SaveFolderPath)); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand FetchTleCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        public MainViewModel()
        {
            AddCommand = new RelayCommand(_ => AddMapping());
            RemoveCommand = new RelayCommand(_ => RemoveMapping(), _ => SelectedMapping != null);
            FetchTleCommand = new RelayCommand(async _ => await FetchAndProcessTle());
            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());

            LoadSettings();
            LoadMappings();
        }

        private void LoadSettings()
        {
            // Читаем папку из пользовательских настроек
            SaveFolderPath = string.IsNullOrWhiteSpace(Properties.Settings.Default.SaveFolderPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Properties.Settings.Default.SaveFolderPath;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.SaveFolderPath = SaveFolderPath;
            Properties.Settings.Default.Save();
        }

        private void LoadMappings()
        {
            var json = Properties.Settings.Default.MappingsJson;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var list = JsonSerializer.Deserialize<SatelliteMapping[]>(json);
                foreach (var m in list) Mappings.Add(m);
            }
        }

        private void SaveMappings()
        {
            var json = JsonSerializer.Serialize(Mappings.ToArray());
            Properties.Settings.Default.MappingsJson = json;
            Properties.Settings.Default.Save();
        }

        private void AddMapping()
        {
            var dlg = new Views.AddMappinngWindow();
            if (dlg.ShowDialog() == true)
            {
                Mappings.Add(new SatelliteMapping
                {
                    NoradId = dlg.NoradId,
                    DisplayName = dlg.SatelliteName
                });
                SaveMappings();
            }
        }

        private void RemoveMapping()
        {
            Mappings.Remove(SelectedMapping);
            SaveMappings();
        }

        private void BrowseFolder()
        {
            using (var fb = new FolderBrowserDialog { SelectedPath = SaveFolderPath })
            {
                if (fb.ShowDialog() == DialogResult.OK)
                {
                    SaveFolderPath = fb.SelectedPath;
                    SaveSettings();
                }
            }
        }

        private async Task FetchAndProcessTle()
        {
            try
            {
                // 1) Настраиваем обработчик с куки
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer()
                };

                // 2) Создаём HttpClient и всё, что с ним связано, внутри using‑блока
                using (var client = new HttpClient(handler)
                { BaseAddress = new Uri("https://www.space-track.org/") })
                {
                    // 3) Авторизация
                    var loginData = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("identity", Properties.Settings.Default.SpacetrackLogin),
                            new KeyValuePair<string, string>("password",  Properties.Settings.Default.SpacetrackPassword)
                        });
                    var loginResp = await client.PostAsync("ajaxauth/login", loginData);
                    loginResp.EnsureSuccessStatusCode();

                    // 4) Формирование списка ID
                    string idList = string.Join(",", Mappings.Select(m => m.NoradId));
                    string tleUrl = $"basicspacedata/query/class/tle_latest/ORDINAL/1/NORAD_CAT_ID/{idList}/format/tle";

                    // 5) Загрузка сырых TLE
                    string tleRaw = await client.GetStringAsync(tleUrl);

                    // 7) Сохранение в выбранную папку
                    // Папка и шаблон имени из настроек, с проверкой
                    var folder = string.IsNullOrWhiteSpace(Properties.Settings.Default.SaveFolderPath)
                        ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TLE")
                        : Properties.Settings.Default.SaveFolderPath;
                    Directory.CreateDirectory(folder);

                    var template = Properties.Settings.Default.DefaultFileName;
                    var fileName = template.Replace("{yyyyMMdd_HHmmss}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    var fullPath = Path.Combine(folder, fileName);

                    // Обработка TLE-блоков без лишних строк, с поддержкой всех строк
                    var lines = tleRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var sb = new StringBuilder();

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];

                        if (line.StartsWith("1 ") && int.TryParse(line.Substring(2, 5), out int id))
                        {
                            var map = Mappings.FirstOrDefault(m => m.NoradId == id);
                            if (map != null)
                                sb.Append(map.DisplayName + "\r\n");
                        }

                        sb.Append(line + "\r\n");
                    }


                    File.WriteAllText(fullPath, sb.ToString());

                    System.Windows.MessageBox.Show($"TLE сохранён:\n{fileName}", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                } // конец using(client)
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}

