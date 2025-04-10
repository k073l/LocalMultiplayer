using System;
using System.Linq;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.Properties;
using ScheduleOne.Property;
using UnityEngine;

namespace RealRadio.Components.Building;

public abstract class OffGridItem : BuildableItem
{
    public void InitializeOffGridItem(ItemInstance itemInstance, Vector3 position, Quaternion rotation, Guid guid)
    {
        if (Initialized)
            return;

        // todo: implement the below function manually to avoid property requirement
        InitializeBuildableItem(itemInstance, guid.ToString(), parentPropertyCode: Business.Businesses.First().PropertyCode);

        transform.position = position;
        transform.rotation = rotation;

        Initialized = true;
    }
}
