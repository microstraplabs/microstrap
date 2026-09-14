using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public sealed class GamesViewModel : NotifyPropertyChangedViewModel
    {
        private const int MaxGames = GamesServiceLimits.MaxGames;

        private bool _isLoading;
        private string _status = "Discovering games...";
        private string _searchTerm = "";
        private bool _hasMoreGames = true;
        private bool _isLoadingPage;
        private CancellationTokenSource? _searchDebounce;

        public ObservableCollection<GameCardViewModel> Games { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

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

        public ICommand RefreshCommand { get; }

        public ICommand LoadMoreGamesCommand { get; }

        public GamesViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadGamesAsync);
            LoadMoreGamesCommand = new AsyncRelayCommand(LoadMoreGamesAsync);
            _ = LoadGamesAsync();
        }

        private async Task LoadGamesAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                RobloxGamesService.ResetSession();

                Games.Clear();
                _hasMoreGames = true;

                Status = "Discovering games...";

                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GamesViewModel::LoadGamesAsync", ex);
                Status = "Roblox could not load game recommendations. Check your connection and try again.";
                _hasMoreGames = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMoreGamesAsync()
        {
            if (!_hasMoreGames || _isLoadingPage || _isLoading || Games.Count >= MaxGames)
                return;

            _isLoadingPage = true;

            try
            {
                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GamesViewModel::LoadMoreGamesAsync", ex);
                Status = "Roblox could not load more games.";
                _hasMoreGames = false;
            }
            finally
            {
                _isLoadingPage = false;
            }
        }

        private async Task FetchAndAppendPageAsync()
        {
            var games = await RobloxGamesService.GetGamesPageAsync(SearchTerm.Trim());

            foreach (var game in games)
            {
                if (Games.Count >= MaxGames)
                    break;

                if (game.PlaceId == 0 || Games.Any(x => x.Game.PlaceId == game.PlaceId))
                    continue;

                Games.Add(new GameCardViewModel(game));
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

                await Application.Current.Dispatcher.InvokeAsync(async () => await LoadGamesAsync());
            });
        }
    }

    public sealed class GameCardViewModel : NotifyPropertyChangedViewModel
    {
        public RecommendedGame Game { get; }

        public string Name => Game.Name;
        public string Creator => String.IsNullOrWhiteSpace(Game.Creator) ? "Roblox experience" : Game.Creator;
        public string Description => String.IsNullOrWhiteSpace(Game.Description) ? "Jump in and start playing." : Game.Description;
        public string ThumbnailUrl => Game.ThumbnailUrl ?? "";
        public string PlayingText => FormatNumber(Game.Playing);
        public string VisitsText => FormatNumber(Game.Visits);

        public ICommand PlayCommand { get; }

        public GameCardViewModel(RecommendedGame game)
        {
            Game = game;
            PlayCommand = new RelayCommand(Play);
        }

        private void Play()
        {
            LaunchHandler.LaunchGame(Game);
        }

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
}
