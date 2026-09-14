using System.Windows;

namespace Bloxstrap.GameManager
{
    public partial class GameDetailWindow : Wpf.Ui.Controls.UiWindow
    {
        public GameDetailWindow(RecommendedGame game)
        {
            InitializeComponent();
            DataContext = new GameDetailViewModel(game);
        }
    }

    public class GameDetailViewModel
    {
        public RecommendedGame Game { get; }

        public string PlayingText => FormatNumber(Game.Playing);
        public string VisitsText => FormatNumber(Game.Visits);
        public string FavoritesText => FormatNumber(Game.FavoritedCount);
        public string DescriptionText => String.IsNullOrWhiteSpace(Game.Description) ? "This experience doesn't have a description yet." : Game.Description;
        public string UpdatedText => Game.Updated == default ? "unknown" : Game.Updated.ToLocalTime().ToString("d MMMM yyyy");

        public RelayCommand PlayCommand { get; }

        public GameDetailViewModel(RecommendedGame game)
        {
            Game = game;
            PlayCommand = new RelayCommand(_ => GameManagerLauncher.Launch(Game.PlaceId));
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
