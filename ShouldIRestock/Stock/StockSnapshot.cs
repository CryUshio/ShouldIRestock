using System.Collections.Generic;

namespace ShouldIRestock
{
    public sealed class StockPiece
    {
        public GameItem Item;
        public GameItemElement Element;
        public bool Stolen;
        public bool Contraband;
        public List<string> Tags;
    }

    public sealed class TagRow
    {
        public string Id;
        public string Name;
        public int Count;
        public int Stolen;
        public int Contraband;
    }

    public sealed class StockSnapshot
    {
        public List<StockPiece> Pieces = new List<StockPiece>();
        public List<TagRow> Rows = new List<TagRow>();
        public int Stolen;
        public int Contraband;
        public int Other;
        public string Signature = "";
    }
}
