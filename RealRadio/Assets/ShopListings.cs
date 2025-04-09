using System;
using System.Collections.Generic;
using ScheduleOne.NPCs;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace RealRadio.Assets;

[Serializable]
[CreateAssetMenu(fileName = "ShopListings", menuName = "RealRadio/ScriptableObjects/ShopListings", order = 1)]
public class ShopListings : ScriptableObject
{
    [SerializeField]
    public ShopListing[] Listings = [];
}
