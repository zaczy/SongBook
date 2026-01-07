using MahApps.Metro.Controls;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new ViewModel();
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
        /// Aktualizuje podgląd piosenki
        /// </summary>
        private void UpdateSongVisualization()
        {
            if (ViewModel?.ConvertedSong != null)
            {
                var visualization = new SongVisualization();
                visualization.MonoFontPath = @"css\Monofonto Regular\Monofonto Regular.ttf";
                PreviewBrowser.NavigateToString(visualization.LyricsHtml(ViewModel!.ConvertedSong));
            }

        }

        /// <summary>
        /// Zapisuje aktualną piosenkę do bazy danych
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var factory = new SongBookDbContextFactory();

            var songRepository = new SongRepository(factory.CreateDbContext(new string[] { }));

            if (ViewModel.ConvertedSong != null)
            {
                var songEntity = await songRepository.SearchOnlySongAsync(ViewModel.ConvertedSong);

                if (songEntity == null)
                    await songRepository.AddAsync(ViewModel.ConvertedSong);
                else
                {
                    songEntity.initFromSong(ViewModel.ConvertedSong);
                    await songRepository.UpdateAsync(songEntity);

                    await ViewModel.LoadSongsAsync();

                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadSongsAsync();
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
        private void SongsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SongsDataGrid.SelectedItem is SongEntity selectedSong)
            {
                // Załaduj wybraną piosenkę do edycji
                ViewModel.LoadSongFromEntity(selectedSong);
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
    }
}