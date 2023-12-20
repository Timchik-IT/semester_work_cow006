using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Game.Models;
using ReactiveUI;

namespace Game.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public Player Player { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ReactiveCommand<Unit, Unit> EndTurnCommand { get; }

    public MainWindowViewModel()
    {
        Initialize();
        Player = new Player();
        ConnectCommand = ReactiveCommand.Create(Connect);
        EndTurnCommand = ReactiveCommand.Create(EndTurn);
    }

    private void EndTurn()
    {
        if (Player.PlayerReady)
            Player.EndTurn();
    }

    private void Connect() => Task.Run(() => Player.Connect());

    private void Initialize()
    {
        var files = Directory.GetFiles("../../../Assets/Rules/");
        var images = files.Select(x => new Bitmap(new FileInfo(x).FullName));
        RuleImages = new ObservableCollection<Bitmap>(images);
    }

    public ObservableCollection<Bitmap>? RuleImages { get; set; }
}