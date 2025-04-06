using ScheduleOne;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace RealRadio.Assets;

public class ItemDefinitions
{
    public BuildableItemDefinition RadioTier1;

    public ItemDefinitions()
    {
        RadioTier1 = CreateRadioDefinition("radio_t1", "Scuffed Radio", "A radio with low quality audio.", AssetRegistry.RadioPrefabs.RadioTier1);
    }

    private BuildableItemDefinition CreateRadioDefinition(string id, string name, string description, BuildableItem buildableItem)
    {
        var radio = ScriptableObject.CreateInstance<BuildableItemDefinition>();
        radio.ID = id;
        radio.name = name;
        radio.Name = name;
        radio.Description = description;
        radio.Category = EItemCategory.Furniture;
        radio.legalStatus = ELegalStatus.Legal;
        radio.StackLimit = 10;
        radio.BuildSoundType = BuildableItemDefinition.EBuildSoundType.Metal;
        radio.BuiltItem = buildableItem;
        radio.BasePurchasePrice = 200f;
        radio.ShopCategories = [new ShopListing.CategoryInstance { Category = EShopCategory.Furniture }];

        Registry.instance.AddToRegistry(radio);
        return radio;
    }
}
