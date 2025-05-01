
using System;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace RealRadio.Components.Vehicles
{
    public class VehicleRadioProxy : NetworkBehaviour
    {
        public LandVehicle Vehicle { get; set; } = null!;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (IsServer)
            {
                if (Vehicle == null)
                    throw new InvalidOperationException("Vehicle is null");

                OnVehicleSet();
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            RequestVehicleInfo(NetworkObject.LocalConnection);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestVehicleInfo(NetworkConnection conn)
        {
            ReceiveVehicleInfo(conn, Vehicle.gameObject);
        }

        [TargetRpc]
        private void ReceiveVehicleInfo(NetworkConnection conn, GameObject vehicleObject)
        {
            Vehicle = vehicleObject.GetComponent<LandVehicle>() ?? throw new InvalidOperationException($"Could not find LandVehicle component on received vehicle object ({vehicleObject?.name})");
            OnVehicleSet();
        }

        private void OnVehicleSet()
        {
            name = $"{name} ({Vehicle.name})";

            Vehicle.onVehicleStart.AddListener(OnVehicleStart);
            Vehicle.onVehicleStop.AddListener(OnVehicleStop);
            Vehicle.onPlayerEnterVehicle += OnPlayerEnterVehicle;
            Vehicle.onPlayerExitVehicle += OnPlayerExitVehicle;
        }

        private void Update()
        {
            if (Vehicle == null)
                return;

            if (IsServer)
            {
                if (!Vehicle)
                    Despawn(DespawnType.Destroy);
            }
        }

        private void OnVehicleStart()
        {
            // The game currently calls OnVehicleStart after a client exits the vehicle
            // We can work around it by checking the number of occupants
            var occupants = Vehicle.OccupantPlayers.Count() + Vehicle.OccupantNPCs.Count(x => x != null);
            OnEngineToggled(occupants > 0);
        }

        private void OnVehicleStop()
        {
            // The game currently calls OnVehicleStart after a client exits the vehicle
            // We can work around it by checking the number of occupants
            var occupants = Vehicle.OccupantPlayers.Count() + Vehicle.OccupantNPCs.Count(x => x != null);
            OnEngineToggled(occupants > 0);
        }

        private void OnEngineToggled(bool started)
        {
            Plugin.Logger.LogInfo($"Engine toggled: {started}");
        }

        private void OnPlayerEnterVehicle(Player player)
        {
            if (player == null || !player.IsLocalPlayer)
                return;

            Plugin.Logger.LogInfo("Local player entered vehicle");
        }

        private void OnPlayerExitVehicle(Player player)
        {
            if (player == null || !player.IsLocalPlayer)
                return;

            Plugin.Logger.LogInfo("Local player exited vehicle");
        }
    }
}
