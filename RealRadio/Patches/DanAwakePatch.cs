using HarmonyLib;
using ScheduleOne.NPCs.CharacterClasses;

namespace RealRadio.Patches;

[HarmonyPatch(typeof(Dan), nameof(Dan.Awake))]
public static class DanAwakePatch
{
    private static void Prefix(Dan __instance)
    {
        foreach (var shopListing in Plugin.Assets!.ShopListings.Listings)
        {
            __instance.ShopInterface.CreateListingUI(shopListing);
        }
    }
}
