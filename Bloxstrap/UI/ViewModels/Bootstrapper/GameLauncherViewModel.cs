using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Shell;

using Bloxstrap.UI.ViewModels;

namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public sealed class GameLauncherViewModel : NotifyPropertyChangedViewModel
    {
        private const int MaxGames = GamesServiceLimits.MaxGames;

        private readonly ObservableCollection<RecommendedGame> _allGames = new();
        private string _searchTerm = "";
        private string _searchStatus = "";
        private RecommendedGame _selectedGame = null!;
        private string _message = "Preparing Roblox...";

        private string _activeQuery = "";
        private bool _hasMoreGames = true;
        private bool _isLoadingPage;
        private bool _reloadRequested;
        private CancellationTokenSource? _searchDebounce;

        public GameLauncherViewModel(RecommendedGame game)
        {
            PinnedGame = game;
            SelectedGame = game;

            // The experience we were asked to launch is always available, even if
            // discovery fails (e.g. no internet) so the normal launch still works.
            _allGames.Add(game);
        }

        /// <summary>
        /// The experience that was requested to launch. It stays pinned at the
        /// top of the recommendations list.
        /// </summary>
        public RecommendedGame PinnedGame { get; }

        public RecommendedGame SelectedGame
        {
            get => _selectedGame;
            private set
            {
                _selectedGame = value;
                OnPropertyChanged(nameof(SelectedGame));
                OnPropertyChanged(nameof(GameName));
                OnPropertyChanged(nameof(Developer));
                OnPropertyChanged(nameof(GameIcon));
            }
        }

        public string GameName => SelectedGame.Name;
        public string Developer => String.IsNullOrWhiteSpace(SelectedGame.Creator) ? "Roblox experience" : SelectedGame.Creator;
        public string GameIcon => SelectedGame.ThumbnailUrl ?? "pack://application:,,,/Microstrap.ico";

        public ObservableCollection<RecommendedGame> Games => _allGames;

        public bool HasMoreGames => _hasMoreGames && _allGames.Count < MaxGames;

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

        public string SearchStatus
        {
            get => _searchStatus;
            private set
            {
                _searchStatus = value;
                OnPropertyChanged(nameof(SearchStatus));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        private int _progressValue;
        private int _progressMaximum = 10000;
        private bool _progressIndeterminate = true;
        private bool _cancelEnabled;
        private TaskbarItemProgressState _taskbarProgressState = TaskbarItemProgressState.Indeterminate;
        private double _taskbarProgressValue;

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public int ProgressMaximum
        {
            get => _progressMaximum;
            set
            {
                _progressMaximum = value;
                OnPropertyChanged(nameof(ProgressMaximum));
            }
        }

        public bool ProgressIndeterminate
        {
            get => _progressIndeterminate;
            set
            {
                _progressIndeterminate = value;
                OnPropertyChanged(nameof(ProgressIndeterminate));
            }
        }

        public bool CancelEnabled
        {
            get => _cancelEnabled;
            set
            {
                _cancelEnabled = value;
                OnPropertyChanged(nameof(CancelEnabled));
            }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _taskbarProgressState;
            set
            {
                _taskbarProgressState = value;
                OnPropertyChanged(nameof(TaskbarProgressState));
            }
        }

        public double TaskbarProgressValue
        {
            get => _taskbarProgressValue;
            set
            {
                _taskbarProgressValue = value;
                OnPropertyChanged(nameof(TaskbarProgressValue));
            }
        }

        /// <summary>
        /// Loads the initial list of games from Roblox.
        /// </summary>
        public async Task InitializeAsync() => await ReloadGamesAsync();

        /// <summary>
        /// Rebuilds the whole list from Roblox with the current search term,
        /// restarting the session. Called when the search changes.
        /// </summary>
        public async Task ReloadGamesAsync()
        {
            if (_isLoadingPage)
            {
                _reloadRequested = true;
                return;
            }

            _isLoadingPage = true;

            try
            {
                RobloxGamesService.ResetSession();

                _activeQuery = _searchTerm.Trim();
                _hasMoreGames = true;

                _allGames.Clear();

                if (String.IsNullOrEmpty(_activeQuery) && PinnedGame is not null)
                    _allGames.Add(PinnedGame);

                SearchStatus = "Loading games...";

                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GameLauncherViewModel::ReloadGamesAsync", ex);
                SearchStatus = "Couldn't load games from Roblox.";
                _hasMoreGames = false;
            }
            finally
            {
                _isLoadingPage = false;
            }

            if (_reloadRequested)
            {
                _reloadRequested = false;
                await ReloadGamesAsync();
            }
        }

        /// <summary>
        /// Loads the next page of games from Roblox, appending to the list.
        /// Scrolling to the bottom of the list calls this, which is why games
        /// keep being recommended up until the listing cap is hit.
        /// </summary>
        public async Task LoadMoreGamesAsync()
        {
            if (!_hasMoreGames || _isLoadingPage || _allGames.Count >= MaxGames)
                return;

            _isLoadingPage = true;

            try
            {
                await FetchAndAppendPageAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GameLauncherViewModel::LoadMoreGamesAsync", ex);
                SearchStatus = "Couldn't load more games from Roblox.";
                _hasMoreGames = false;
            }
            finally
            {
                _isLoadingPage = false;
            }

            if (_reloadRequested)
            {
                _reloadRequested = false;
                await ReloadGamesAsync();
            }
        }

        private async Task FetchAndAppendPageAsync()
        {
            var games = await RobloxGamesService.GetGamesPageAsync(_activeQuery);

            // the query changed while we were fetching, so this page is stale
            if (!_activeQuery.Equals(_searchTerm.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _reloadRequested = true;
                return;
            }

            foreach (var game in games)
            {
                if (_allGames.Count >= MaxGames)
                    break;

                if (game.PlaceId == 0)
                    continue;

                if (_allGames.Any(x => x.PlaceId == game.PlaceId))
                    continue;

                if (PinnedGame is not null && game.PlaceId == PinnedGame.PlaceId)
                    continue;

                _allGames.Add(game);
            }

            _hasMoreGames = games.Count > 0 && _allGames.Count < MaxGames;

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_allGames.Count >= MaxGames)
                SearchStatus = "No more games on the list";
            else if (!_hasMoreGames)
                SearchStatus = "No more games on the list";
            else if (_isLoadingPage)
                SearchStatus = "Loading more games...";
            else
                SearchStatus = $"{_allGames.Count} games";
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

        public void SelectGame(RecommendedGame game)
        {
            if (game is null || game.PlaceId == SelectedGame.PlaceId)
                return;

            SelectedGame = game;
        }
    }
}
