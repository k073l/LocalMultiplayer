using System;
using RealRadio.Assets.Prefabs;
using ScheduleOne;

namespace RealRadio.Assets;

public static class AssetRegistry
{
    public static ItemDefinitions ItemDefinitions { get; private set; } = null!;
    public static ShopListings ShopListings { get; private set; } = null!;
    public static RadioPrefabs RadioPrefabs { get; private set; } = null!;

    private static bool initialized;

    public static void Initialize(Registry registry)
    {
        if (initialized)
            return;

        initialized = true;

        // IMPORTANT: Some of these may depend on each other, so the order that these are created in matters.
        RadioPrefabs = new RadioPrefabs(registry);
        ItemDefinitions = new ItemDefinitions(registry);
        ShopListings = new ShopListings();
    }
}
