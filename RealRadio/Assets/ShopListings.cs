using ScheduleOne.UI.Shop;

namespace RealRadio.Assets;

public class ShopListings
{
    public ShopListing RadioTier1 { get; set; }

    public ShopListings()
    {
        RadioTier1 = new ShopListing
        {
            CanBeDelivered = true,
            name = "Scuffed Radio",
            Item = AssetRegistry.ItemDefinitions.RadioTier1,
            StockQuantity = 10
        };
    }
}
