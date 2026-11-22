using System.Collections.Generic;

namespace ShouldIRestock
{
    public sealed class StockPiece
    {
        public GameItem Item;
        public GameItemElement Element;
        public bool Stolen;
        public bool Contraband;
        public bool Excluded;
        public List<string> Tags;

        // 容器里面（含多层嵌套）的货，用来让外层容器也跟着高亮。
        public List<StockPiece> Inner = new List<StockPiece>();

        public bool Own(string tag)
        {
            if (tag == ModIds.Stolen) return Stolen;
            if (tag == ModIds.Contraband) return Contraband;
            if (Excluded) return false;
            if (tag == ModIds.Other) return Tags.Count == 0;
            return Tags.Contains(tag);
        }

        public bool Has(string tag)
        {
            return Style(tag) != HighlightStyle.None;
        }

        // 和 hollow.clientdemandhighlight 一致：违禁品 > 赃物 > 普通；容器取里面命中的最严重的那件。
        public HighlightStyle Style(string tag)
        {
            HighlightStyle best = StyleOf(tag);
            for (int i = 0; i < Inner.Count && best != HighlightStyle.Contraband; i++)
            {
                HighlightStyle inner = Inner[i].StyleOf(tag);
                if (inner > best) best = inner;
            }

            return best;
        }

        private HighlightStyle StyleOf(string tag)
        {
            if (!Own(tag)) return HighlightStyle.None;
            if (Contraband) return HighlightStyle.Contraband;
            return Stolen ? HighlightStyle.Stolen : HighlightStyle.Normal;
        }
    }

    public enum HighlightStyle
    {
        None,
        Normal,
        Stolen,
        Contraband
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
        public Dictionary<GameItem, StockPiece> Index = new Dictionary<GameItem, StockPiece>();
        public List<TagRow> Rows = new List<TagRow>();
        public int Stolen;
        public int Contraband;
        public int Other;
        public string Signature = "";
    }
}

