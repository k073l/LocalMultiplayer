using System;
using RealRadio.Components.Buildings;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class BuildingRadioManager : NetworkSingleton<BuildingRadioManager>
{
    [field: SerializeField]
    public GameObject RadioProxyPrefab { get; private set; } = null!;

    public override void Awake()
    {
        base.Awake();

        if (RadioProxyPrefab == null)
            throw new InvalidOperationException("RadioProxyPrefab is null");
    }

    public override void Start()
    {
        base.Start();
    }
}
