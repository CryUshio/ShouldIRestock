using System.Collections.Generic;
using System.Text;

namespace ShouldIRestock
{
    public static class StockScanner
    {
        public static StockSnapshot Scan()
        {
            var shot = new StockSnapshot();
            EmporiumEntry store = null;
            try
            {
                store = EmporiumEntry.Instance;
            }
            catch
            {
                store = null;
            }

            if (store == null)
            {
                return shot;
            }

            var seen = new HashSet<GameItem>();
            WalkInventory(Read(() => store.invElement), seen, shot);
            WalkInventory(Read(() => store.frontInvinvElement), seen, shot);
            WalkInventory(Read(() => store.backInvinvElement), seen, shot);
            WalkInventory(Read(() => store.backInvinvElementCounter), seen, shot);
            WalkInventory(Read(() => store.showcaseElement), seen, shot);
            WalkInventory(Read(() => store.bazarLeftinvElement), seen, shot);
            WalkInventory(Read(() => store.hiddenElement), seen, shot);
            BuildRows(shot);
            return shot;
        }

        private static void WalkInventory(GameInventory inventory, HashSet<GameItem> seen, StockSnapshot shot)
        {
            if (inventory == null)
            {
                return;
            }

            List<GameItem> items = null;
            try
            {
                items = inventory.childItems;
            }
            catch
            {
                items = null;
            }

            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                WalkItem(items[i], seen, shot);
            }
        }

        private static void WalkItem(GameItem item, HashSet<GameItem> seen, StockSnapshot shot)
        {
            if (item == null || !seen.Add(item))
            {
                return;
            }

            if (Settings.IgnoreLocked && Legality.HiddenByLock(item))
            {
                return;
            }

            if (Settings.SkipDebris && Legality.IsDebris(item))
            {
                return;
            }

            if (Legality.IsOwned(item))
            {
                List<string> tags = TagCatalog.Match(item);
                bool excluded = Settings.IsExcluded(item, tags);
                if (!excluded || Settings.KeepsLegality)
                {
                    var piece = new StockPiece
                    {
                        Item = item,
                        Element = item as GameItemElement,
                        Stolen = Legality.IsStolen(item),
                        Contraband = Legality.IsContraband(item),
                        Excluded = excluded,
                        Tags = tags ?? new List<string>()
                    };
                    shot.Pieces.Add(piece);
                    shot.Index[item] = piece;
                }
            }

            if (Settings.IgnoreLocked && Legality.IsLockedContainer(item))
            {
                return;
            }

            if (TagCatalog.IsWineInProgress(item))
            {
                return;
            }

            List<GameItem> children = null;
            try
            {
                children = item.FindAllChildItems();
            }
            catch
            {
                children = null;
            }

            if (children == null)
            {
                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                WalkItem(children[i], seen, shot);
            }

            StockPiece self;
            if (!shot.Index.TryGetValue(item, out self))
            {
                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                StockPiece inner;
                if (children[i] == null || !shot.Index.TryGetValue(children[i], out inner))
                {
                    continue;
                }

                self.Inner.Add(inner);
            }
        }

        private static void BuildRows(StockSnapshot shot)
        {
            var counts = new Dictionary<string, TagRow>();
            var other = new TagRow { Id = ModIds.Other, Name = Text.L("tag_other", "Uncategorized") };
            var stolenRow = new TagRow { Id = ModIds.Stolen, Name = Text.L("all_stolen", "All stolen") };
            var contrabandRow = new TagRow { Id = ModIds.Contraband, Name = Text.L("all_contraband", "All contraband") };
            for (int i = 0; i < shot.Pieces.Count; i++)
            {
                StockPiece piece = shot.Pieces[i];
                int units = Units(piece.Item);
                long worth = ItemWorth.Of(piece.Item);
                if (piece.Stolen)
                {
                    Add(stolenRow, piece, units, worth);
                }

                if (piece.Contraband)
                {
                    Add(contrabandRow, piece, units, worth);
                }

                if (piece.Excluded)
                {
                    continue;
                }

                if (piece.Tags.Count == 0)
                {
                    Add(other, piece, units, worth);
                    continue;
                }

                for (int t = 0; t < piece.Tags.Count; t++)
                {
                    if (!Settings.Shows(piece.Tags[t]))
                    {
                        continue;
                    }

                    TagRow row;
                    if (!counts.TryGetValue(piece.Tags[t], out row))
                    {
                        row = new TagRow { Id = piece.Tags[t], Name = TagCatalog.NameOf(piece.Tags[t]) };
                        counts[row.Id] = row;
                    }

                    Add(row, piece, units, worth);
                }
            }

            shot.Stolen = stolenRow.Count;
            shot.Contraband = contrabandRow.Count;
            shot.Other = other.Count;

            // 勾选即占一行，没有库存也显示 0，这样开关会立刻反映到便签上。
            List<string> shown = ShownTypes.IdsInOrder();
            for (int i = 0; i < shown.Count; i++)
            {
                string id = shown[i];
                if (id == ModIds.Stolen || id == ModIds.Contraband || id == ModIds.Other || counts.ContainsKey(id) || !Settings.Shows(id))
                {
                    continue;
                }

                counts[id] = new TagRow { Id = id, Name = TagCatalog.NameOf(id) };
            }

            var rows = new List<TagRow>();
            if (Settings.Shows(ModIds.Stolen))
            {
                rows.Add(stolenRow);
            }

            if (Settings.Shows(ModIds.Contraband))
            {
                rows.Add(contrabandRow);
            }

            var types = new List<TagRow>(counts.Values);
            types.Sort((a, b) =>
            {
                int byOrder = TagCatalog.OrderOf(a.Id).CompareTo(TagCatalog.OrderOf(b.Id));
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Id, b.Id);
            });
            rows.AddRange(types);

            if (Settings.Shows(ModIds.Other))
            {
                rows.Add(other);
            }

            shot.Rows = rows;
            var signature = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0) signature.Append('|');
                signature.Append(rows[i].Id);
            }

            shot.Signature = signature.ToString();
        }

        // 普通、赃物、违禁品分开计。又脏又违禁的货两边都算，不合成一个总数。
        private static void Add(TagRow row, StockPiece piece, int units, long worth)
        {
            row.Count += units;
            row.Value += worth;
            if (piece.Stolen)
            {
                row.Stolen += units;
            }

            if (piece.Contraband)
            {
                row.Contraband += units;
            }

            if (!piece.Stolen && !piece.Contraband)
            {
                row.Normal += units;
            }
        }

        private static int Units(GameItem item)
        {
            try
            {
                int count = item != null ? item.unitCount : 1;
                return count > 0 ? count : 1;
            }
            catch
            {
                return 1;
            }
        }

        private static GameInventory Read(System.Func<GameInventory> get)
        {
            try
            {
                return get();
            }
            catch
            {
                return null;
            }
        }
    }
}
