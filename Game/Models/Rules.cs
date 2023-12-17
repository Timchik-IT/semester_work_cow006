using Avalonia.Media.Imaging;

namespace Game.Models;

public class Rules
{
    public Rules(string ruleImagePath) => Bitmap = new Bitmap(ruleImagePath);
    
    public Bitmap Bitmap { get; set; }
}