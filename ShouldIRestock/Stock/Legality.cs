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
    }
}
