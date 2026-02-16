using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.WPF
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private bool _webViewInitialized;
        private string? appAssetsPath;

        public MainWindow(ViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            // Set DataContext to the ViewModel instance so XAML bindings can be simple (e.g. {Binding Songs})
            DataContext = _viewModel;
        }

        private ViewModel _viewModel;
        public ViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != value)
                {
                    _viewModel = value;
                    OnPropertyChanged(nameof(ViewModel));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Załaduj piosenki z DB (ViewModel)
            await ViewModel.LoadSongsAsync();

            // Inicjalizuj WebView2 jednorazowo
            await EnsureWebView2InitializedAsync();

        }

        /// <summary>
        /// Sprawdza i inicjalizuje WebView2 jeśli to konieczne
        /// </summary>
        /// <returns></returns>
        private async Task EnsureWebView2InitializedAsync()
        {
            if (_webViewInitialized)
                return;

            try
            {
                // Utwórz CoreWebView2 jeśli jeszcze nie ma
                await PreviewWebView.EnsureCoreWebView2Async();

                // Mapuj lokalny folder zasobów do wirtualnego hosta, dzięki temu można korzystać z https://appassets/...
                appAssetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                Directory.CreateDirectory(appAssetsPath); // upewnij się, że folder istnieje

                PreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets",
                    appAssetsPath,
                    CoreWebView2HostResourceAccessKind.Allow);

                _webViewInitialized = true;
            }
            catch
            {
                // nie blokujemy aplikacji — brak WebView2 runtime lub inne problemy
                _webViewInitialized = false;
            }
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string htmlContent = File.ReadAllText(openFileDialog.FileName);

                if (!string.IsNullOrEmpty(htmlContent))
                {
                    ViewModel.SourceSongHtml = htmlContent;
                    ViewModel.ConvertedSong = Song.CreateFromW(ViewModel.SourceSongHtml);

                    UpdateSongVisualization();
                }
            }
        }

        private void SongTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ViewModel.SourceSongHtml))
            {
                ViewModel.ConvertedSong = Song.CreateFromW(ViewModel.SourceSongHtml);
            }
        }

        /// <summary>
        /// Aktualizuje podgląd piosenki (WebView2)
        /// </summary>
        private async void UpdateSongVisualization()
        {
            if (ViewModel?.ConvertedSong == null)
                return;

            // Upewnij się, że WebView2 zainicjalizowany
            await EnsureWebView2InitializedAsync();

            var visualization = new SongVisualization();

            // Przyjmujemy, że lokalne zasoby (css, czcionki) znajdą się w folderze "<app>/assets"
            // Skopiuj do projektu zasoby np. assets/css/... i ustaw CopyToOutputDirectory=CopyIfNewer
            var virtualBase = "https://appassets/"; // odpowiada SetVirtualHostNameToFolderMapping
            visualization.CssFontsPath = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(appAssetsPath))
            {
                //string fontFile = "css/Monofonto Regular/Monofonto Regular.ttf";
                string fontFile = "css/Inconsolata/Inconsolata-VariableFont_wdth,wght.ttf";
                if (File.Exists(Path.Combine(appAssetsPath, fontFile)))
                {
                    // jeśli pliki istnieją w katalogu assets, użyj ścieżek do nich
                    visualization.CssFontsPath.Add("CustomFixedFont", virtualBase + fontFile);
                }

                fontFile = "css/Roboto/Roboto-VariableFont_wdth,wght.ttf";
                if (File.Exists(Path.Combine(appAssetsPath, fontFile)))
                {
                    visualization.CssFontsPath.Add("RobotoVariable", virtualBase + fontFile);
                }

                fontFile = "css/Poltawski_Nowy/PoltawskiNowy-VariableFont_wght.ttf";
                if (File.Exists(Path.Combine(appAssetsPath, fontFile)))
                {
                    visualization.CssFontsPath.Add("PoltawskiVariable", virtualBase + fontFile);
                }
            }

            var html = visualization.LyricsHtml(ViewModel.ConvertedSong, ViewModel.LyricsHtmlVersion);

            // Jeśli WebView2 jest gotowy, użyj NavigateToString
            if (_webViewInitialized && PreviewWebView.CoreWebView2 != null)
            {
                PreviewWebView.NavigateToString(html);
                //PreviewBrowser.NavigateToString(html);
            }
            else
            {
                // Fallback: zapisz do pliku i nawiguj (powinno też działać)
                var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview");
                Directory.CreateDirectory(outDir);
                var fileName = Path.Combine(outDir, $"{Guid.NewGuid():N}.html");
                File.WriteAllText(fileName, html, Encoding.UTF8);
                PreviewWebView.Source = new Uri(fileName);
            }
        }

        /// <summary>
        /// Zapisuje aktualną piosenkę do bazy danych
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(ViewModel?.AppSettings?.ConnectionStrings?.SongBookDb))
                throw new InvalidOperationException("Brak ustawień połączenia do bazy danych.");

            var factory = new SongBookDbContextFactory();
            var songRepository = new SongRepository(factory.CreateDbContext(ViewModel.AppSettings.ConnectionStrings.SongBookDb));

            if (ViewModel.ConvertedSong != null)
            {
                var songEntity = await songRepository.SearchOnlySongAsync(ViewModel.ConvertedSong);

                if (songEntity == null)
                    await songRepository.AddAsync(ViewModel.ConvertedSong);
                else
                {
                    songEntity.initFromSong(ViewModel.ConvertedSong);
                    await songRepository.UpdateAsync(songEntity);

                    //ViewModel.Songs.Clear();
                    //await ViewModel.LoadSongsAsync();

                }
            }
        }

        private async void RefreshSongs_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadSongsAsync();
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            if (SongsDataGrid.SelectedItem is SongEntity selectedSong)
            {
                var result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć piosenkę \"{selectedSong.Title}\"?",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ViewModel.DeleteSongAsync(selectedSong.Id);
                    await ViewModel.LoadSongsAsync();
                }
            }
        }

        /// <summary>
        /// Załaduj wybraną piosenkę do edycji
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SongsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SongsDataGrid.SelectedItem is SongEntity selectedSong)
            {
                // Załaduj wybraną piosenkę do edycji
                await ViewModel.LoadSongFromEntity(selectedSong);
                UpdateSongVisualization();
            }
        }

        /// <summary>
        /// Nowa piosenka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SourceSongHtml = String.Empty;
            ViewModel.ConvertedSong = new Song();
        }

        /// <summary>
        /// Analizuj html źródłowy i pobierz z niego dane piosenki
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnalizujBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ViewModel.SourceSongHtml))
            {
                ViewModel.ConvertedSong = Song.CreateFromW(ViewModel.SourceSongHtml);
                UpdateSongVisualization();
            }
        }

        /// <summary>
        /// Jeden półton w górę
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToneUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ConvertedSong != null)
            {
                ViewModel.ConvertedSong.AdjustTonation(1);
                UpdateSongVisualization();
            }
        }

        /// <summary>
        /// Jeden półton w dół
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToneDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ConvertedSong != null)
            {
                ViewModel.ConvertedSong.AdjustTonation(-1);
                UpdateSongVisualization();
            }
        }

        private async void SyncUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ViewModel?.AppSettings?.Settings?.ApiBaseUrl) && !string.IsNullOrEmpty(ViewModel?.AppSettings?.ConnectionStrings?.SongBookDb))
            {
                SongApi songApi = new SongApi(ViewModel.AppSettings.Settings.ApiBaseUrl);

                var factory = new SongBookDbContextFactory();
                var songRepository = new SongRepository(factory.CreateDbContext(ViewModel.AppSettings.ConnectionStrings.SongBookDb));

                await songApi.SyncApi(songRepository);
            }
        }

        private void HtmlVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre)
            {
                ViewModel.LyricsHtmlVersion = LyricsHtmlVersion.RelativeHtml;
            }
            else
            {
                ViewModel.LyricsHtmlVersion = LyricsHtmlVersion.Pre;
            }

            UpdateSongVisualization();
        }

        private void Filter_TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string filterText = textBox.Text?.Trim() ?? string.Empty;

                // Filtruj tylko jeśli wprowadzono 3 lub więcej znaków
                if (filterText.Length >= 3)
                {
                    var filtered = ViewModel.Songs.Where(song =>
                        (song.Title?.Contains(filterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (song.Artist?.Contains(filterText, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();

                    SongsDataGrid.ItemsSource = filtered;
                }
                else
                {
                    // Jeśli mniej niż 3 znaki, przywróć pełną listę
                    SongsDataGrid.ItemsSource = ViewModel.Songs;
                }
            }
        }
    }
}