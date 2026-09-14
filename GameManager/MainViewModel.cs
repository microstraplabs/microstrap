using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Bloxstrap.GameManager
{
    public class GameCardViewModel : INotifyPropertyChanged
    {
        public RecommendedGame Game { get; }

        public string Name => Game.Name;
        public string Creator => String.IsNullOrWhiteSpace(Game.Creator) ? "Roblox experience" : Game.Creator;
        public string PlayingText => FormatNumber(Game.Playing);
        public string VisitsText => FormatNumber(Game.Visits);
        public string ThumbnailUrl => Game.ThumbnailUrl ?? "";

        public ICommand OpenCommand { get; }
        public ICommand PlayCommand { get; }

        public GameCardViewModel(RecommendedGame game, Action<RecommendedGame> openHandler)
        {
            Game = game;
            OpenCommand = new RelayCommand(_ => openHandler(Game));
            PlayCommand = new RelayCommand(_ => LaunchGame());
        }

        private void LaunchGame()
        {
            GameManagerLauncher.Launch(Game.PlaceId);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private static string FormatNumber(long value)
        {
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000d:0.0}B";
            if (value >= 1_000_000)
                return $"{value / 1_000_000d:0.0}M";
            if (value >= 1_000)
                return $"{value / 1_000d:0.0}K";
            return value.ToString("N0");
        }
    }

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const int MaxGames = GamesServiceLimits.MaxGames;

        private string _searchTerm = "";
        private string _status = "Loading games...";
        private bool _isLoading = true;
        private bool _hasMoreGames = true;
        private bool _isLoadingPage;
        private CancellationTokenSource? _searchDebounce;

        public ObservableCollection<GameCardViewModel> Games { get; } = new();

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged(nameof(SearchTerm));
                ScheduleSearchReload();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ICommand LoadMoreGamesCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand OpenGameCommand { get; }

        public MainViewModel()
        {
            LoadMoreGamesCommand = new RelayCommand(_ => _ = LoadMoreGamesAsync());
            RefreshCommand = new RelayCommand(_ => _ = ReloadGamesAsync());
            OpenGameCommand = new RelayCommand(game =>
            {
                // the card grid passes the card wrapper; anything else is treated as the game itself
                switch (game)
                {
                    case GameCardViewModel card:
                        OpenGame(card.Game);
                        break;
                    case RecommendedGame recommended:
                        OpenGame(recommended);
                        break;
                }
            });
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                Status = "Roblox could not be reached. Check your connection and try again.";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMoreGamesAsync()
        {
            if (!_hasMoreGames || _isLoadingPage || Games.Count >= MaxGames)
                return;

            _isLoadingPage = true;

            try
            {
                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                Status = "Couldn't load more games.";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isLoadingPage = false;
            }
        }

        private async Task FetchAndAppendPageAsync()
        {
            var games = await GamesService.GetGamesPageAsync(SearchTerm);

            foreach (var game in games)
            {
                if (Games.Count >= MaxGames)
                    break;

                if (game.PlaceId == 0 || Games.Any(x => x.Game.PlaceId == game.PlaceId))
                    continue;

                Games.Add(new GameCardViewModel(game, OpenGame));
            }

            _hasMoreGames = games.Count > 0 && Games.Count < MaxGames;

            if (Games.Count >= MaxGames || !_hasMoreGames)
                Status = "No more games on the list";
            else
                Status = $"Showing {Games.Count} games from Roblox";
        }

        private void ScheduleSearchReload()
        {
            _searchDebounce?.Cancel();
            _searchDebounce = new CancellationTokenSource();

            var token = _searchDebounce.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                    return;

                await Application.Current.Dispatcher.InvokeAsync(async () => await ReloadGamesAsync());
            });
        }

        private async Task ReloadGamesAsync()
        {
            if (_isLoadingPage)
                return;

            _isLoadingPage = true;
            IsLoading = true;

            try
            {
                GamesService.ResetSession();
                _hasMoreGames = true;

                Games.Clear();

                Status = "Searching Roblox...";

                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                Status = "Roblox could not be reached. Check your connection and try again.";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isLoadingPage = false;
                IsLoading = false;
            }
        }

        private void OpenGame(RecommendedGame game)
        {
            var window = new GameDetailWindow(game);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        public void Dispose()
        {
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
