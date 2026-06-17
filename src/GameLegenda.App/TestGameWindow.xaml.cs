using System.Windows;
using System.Windows.Threading;

namespace GameLegenda.App;

public partial class TestGameWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly string[] _phrases =
    [
        "Hello traveler. The old bridge is dangerous tonight.",
        "Iron Sword",
        "Quest Updated",
        "Health Potion"
    ];

    private int _index;

    public TestGameWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _timer.Tick += (_, _) => MoveNext();

        SetPhrase();
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    public string CurrentPhrase => _phrases[_index];

    private void OnNextPhraseClick(object sender, RoutedEventArgs e) => MoveNext();

    private void MoveNext()
    {
        _index = (_index + 1) % _phrases.Length;
        SetPhrase();
    }

    private void SetPhrase()
    {
        PhraseText.Text = CurrentPhrase;
    }
}
