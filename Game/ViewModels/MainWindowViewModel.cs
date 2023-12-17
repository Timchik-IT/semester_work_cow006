using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Game.Models;
using ReactiveUI;

namespace Game.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Initialize();
        Player = new Player();
        ConnectCommand = ReactiveCommand.Create(Connect);
    }

    private void Initialize()
    {
        var files = Directory.GetFiles("../../../Assets/Rules/");   
        var images = files.Select(x => new Rules(new FileInfo(x).FullName));
        RuleImages = new ObservableCollection<Rules>(images);
    }

    public string Greeting => "Welcome to Cow 006!";
 
    public Player Player { get; }
    
    private ObservableCollection<Rules>? _ruleImages;
    
    public ObservableCollection<Rules>? RuleImages
    {
        get => _ruleImages;
        set
        {
            if (value != null) _ruleImages = value;
        }
    }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    
    private void Connect() => Task.Run(() => Player.ConnectAsync());
}