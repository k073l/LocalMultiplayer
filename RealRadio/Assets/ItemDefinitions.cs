using ScheduleOne;
using ScheduleOne.EntityFramework;
using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace RealRadio.Assets;

public class ItemDefinitions
{
    public BuildableItemDefinition RadioTier1;

    private readonly Registry registry;
    private readonly Equippable_BuildableItem radioEquippable;

    public ItemDefinitions(Registry registry)
    {
        this.registry = registry;

        radioEquippable = CreateRadioEquippable();
        RadioTier1 = CreateRadioDefinition("radio_t1", "Scuffed Radio", "A radio with low quality audio.", price: 200f, AssetRegistry.RadioPrefabs.RadioTier1);
    }

    private BuildableItemDefinition CreateRadioDefinition(string id, string name, string description, float price, BuildableItem buildableItem)
    {
        id = $"com.skipcast.realradio_{id}";

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
        radio.BasePurchasePrice = price;
        radio.ShopCategories = [new ShopListing.CategoryInstance { Category = EShopCategory.Furniture }];
        radio.Equippable = radioEquippable;

        registry.ItemRegistry.Add(
            new Registry.ItemRegister
            {
                ID = id,
                AssetPath = string.Empty,
                Definition = radio
            }
        );
        return radio;
    }

    private Equippable_BuildableItem CreateRadioEquippable()
    {
        var go = AssetCreationUtil.CreatePrefabObject("RadioEquippable");
        var equippable = go.AddComponent<Equippable_BuildableItem>();
        return equippable;
    }
}
