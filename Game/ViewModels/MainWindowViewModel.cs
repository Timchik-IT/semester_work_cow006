using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Game.Models;
using ReactiveUI;

namespace Game.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public Player Player { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ReactiveCommand<Unit, Unit> EndTurnCommand { get; }
    
    public ReactiveCommand<byte, Unit> SelectCardCommand { get; }

    public ObservableCollection<PlayCard> Cards { get; set; }
    
    public List<ObservableCollection<PlayCard>> DeckLists { get; set; }

    public MainWindowViewModel()
    {
        Initialize();
        Cards = new ObservableCollection<PlayCard>();
        DeckLists = new List<ObservableCollection<PlayCard>>();
        Player = new Player();
        ConnectCommand = ReactiveCommand.Create(Connect);
        EndTurnCommand = ReactiveCommand.Create(EndTurn);
        SelectCardCommand = ReactiveCommand.Create<byte>(SelectCard);
    }

    private void ResetMyCards()
    {
        Cards.Clear();
        foreach (var variableCard in Player.PlayerCards)
        {
            Cards.Add(variableCard);
        }
    }

    private void ResetDeckCards()
    {
        DeckLists.Clear();
        for (var i = 0; i < 4; i++)
        {
            foreach (var card in Player.DeckLists[i])
            {
                DeckLists[i].Add(card);
            }
        }
    }

    private void EndTurn()
    {
        Player.EndTurn();
        Thread.Sleep(10);
        ResetMyCards();
    }

    private void SelectCard(byte idCard) => Player.SelectCard(idCard);

    private void Connect() => Task.Run(() => Player.Connect());

    private void Initialize()
    {
        var files = Directory.GetFiles("../../../Assets/Rules/");
        var images = files.Select(x => new Bitmap(new FileInfo(x).FullName));
        RuleImages = new ObservableCollection<Bitmap>(images);
    }

    public ObservableCollection<Bitmap>? RuleImages { get; set; }
}