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

            if (Legality.IsBehindLock(item))
            {
                return;
            }

            if (Settings.SkipDebris != null && Settings.SkipDebris.Value && Legality.IsDebris(item))
            {
                return;
            }

            if (Legality.IsOwned(item))
            {
                var piece = new StockPiece
                {
                    Item = item,
                    Element = item as GameItemElement,
                    Stolen = Legality.IsStolen(item),
                    Contraband = Legality.IsContraband(item),
                    Tags = TagCatalog.Match(item)
                };
                shot.Pieces.Add(piece);
            }

            if (Settings.IgnoreLocked != null && Settings.IgnoreLocked.Value && Legality.IsLockedContainer(item))
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
        }

        private static void BuildRows(StockSnapshot shot)
        {
            var counts = new Dictionary<string, TagRow>();
            IReadOnlyList<TagInfo> tags = TagCatalog.Tags;
            for (int i = 0; i < tags.Count; i++)
            {
                counts[tags[i].Id] = new TagRow { Id = tags[i].Id, Name = tags[i].Name };
            }

            var other = new TagRow { Id = ModIds.Other, Name = Text.L("tag_other", "未归类") };
            for (int i = 0; i < shot.Pieces.Count; i++)
            {
                StockPiece piece = shot.Pieces[i];
                if (piece.Stolen)
                {
                    shot.Stolen++;
                }

                if (piece.Contraband)
                {
                    shot.Contraband++;
                }

                if (piece.Tags.Count == 0)
                {
                    shot.Other++;
                    other.Count++;
                    if (piece.Stolen) other.Stolen++;
                    if (piece.Contraband) other.Contraband++;
                    continue;
                }

                for (int t = 0; t < piece.Tags.Count; t++)
                {
                    TagRow row;
                    if (!counts.TryGetValue(piece.Tags[t], out row))
                    {
                        continue;
                    }

                    row.Count++;
                    if (piece.Stolen) row.Stolen++;
                    if (piece.Contraband) row.Contraband++;
                }
            }

            var rows = new List<TagRow>();
            for (int i = 0; i < tags.Count; i++)
            {
                TagRow row = counts[tags[i].Id];
                if (row.Count > 0)
                {
                    rows.Add(row);
                }
            }

            if (other.Count > 0)
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
