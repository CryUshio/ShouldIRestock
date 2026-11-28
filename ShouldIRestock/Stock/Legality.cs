namespace ShouldIRestock
{
    public static class Legality
    {
        public static bool IsStolen(GameItem item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                return StolenHelper.IsStolenItem(item);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsContraband(GameItem item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                return ContrabandHelper.IsContraband(item);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsOwned(GameItem item)
        {
            try
            {
                return GeneralHelper.IsItemOwned(item);
            }
            catch
            {
                return true;
            }
        }

        public static bool IsDebris(GameItem item)
        {
            return TagCatalog.Has(item, "DEBRIS_PILE_TAG") || TagCatalog.Has(item, "DEBRIS_TAG");
        }

        public static bool IsBehindLock(GameItem item)
        {
            return TagCatalog.Has(item, "ITEM_BEHIND_LOCK_TAG");
        }

        public static bool IsLockedContainer(GameItem item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                if (item.contentWindow != null && item.contentWindow.isLocked)
                {
                    return true;
                }
            }
            catch
            {
            }

            if (!TagCatalog.Has(item, "LOCKED_CONTAINER_TAG"))
            {
                return false;
            }

            try
            {
                TagState state = item.GetTagReadonly("LOCK_STATUS");
                return state != null && state.GetBool();
            }
            catch
            {
                return true;
            }
        }

        // 锁着的箱子放进别的容器后，扫描会把箱子和里面的货摊平。
        // 游戏只给直接放进去的货打锁后标记，更里面的货要顺着外层容器往上查。
        public static bool HiddenByLock(GameItem item)
        {
            if (item == null || IsBehindLock(item))
            {
                return item != null;
            }

            var seen = new System.Collections.Generic.HashSet<GameItem>();
            GameItem current = item;
            for (int i = 0; i < 16 && current != null && seen.Add(current); i++)
            {
                GameInventory inventory = null;
                try
                {
                    inventory = current.parentInventory;
                }
                catch
                {
                    inventory = null;
                }

                if (inventory == null)
                {
                    return false;
                }

                GameItem owner = null;
                try
                {
                    owner = inventory.GetParentItem();
                }
                catch
                {
                    owner = null;
                }

                if (owner == null || owner == current)
                {
                    return false;
                }

                if (IsLockedContainer(owner))
                {
                    return true;
                }

                current = owner;
            }

            return false;
        }
    }
}
