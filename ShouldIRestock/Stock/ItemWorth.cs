namespace ShouldIRestock
{
    public static class ItemWorth
    {
        // 这件货自己的基础价值，乘上叠数。不算里面的货，那些会记在它们自己的标签上。
        public static long Of(GameItem item)
        {
            try
            {
                if (item == null)
                {
                    return 0;
                }

                int units = item.unitCount > 0 ? item.unitCount : 1;
                long misc = item.GetItemMiscValue(false, false, false, false, false);
                long raw = item.unitValue + item.GetComplexeItemValue() + misc + WaterHelper.GetContainerPrice(item);
                return GameItem.CheckValueSafe(raw) * units;
            }
            catch
            {
                return 0;
            }
        }
    }
}
