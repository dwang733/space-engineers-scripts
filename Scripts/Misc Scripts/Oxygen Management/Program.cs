using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /// <summary>
        /// Matches inner door name with pattern "[Room 1]/[Room 2]".
        /// </summary>
        private System.Text.RegularExpressions.Regex InnerDoorRegex = new System.Text.RegularExpressions.Regex(
            @"(\w+)\/(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches outer door name with pattern "[Room] Outer".
        /// </summary>
        private System.Text.RegularExpressions.Regex OuterDoorRegex = new System.Text.RegularExpressions.Regex(
            @"(\w+)\s+Outer", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches air vent name with pattern "[Room] Vent".
        /// </summary>
        private System.Text.RegularExpressions.Regex VentRegex = new System.Text.RegularExpressions.Regex(
            @"(\w+)\s+Vent", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// The grid terminal system helper.
        /// </summary>
        private readonly GTSHelper _gtsHelper;

        /// <summary>
        /// The airtight rooms that this program manages.
        /// </summary>
        private Dictionary<string, AirtightRoom> _rooms;

        public Program()
        {
            Echo("Script started!");

            _gtsHelper = new GTSHelper(this);
            InitializeRooms();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                {
                    InitializeRooms();
                }

                foreach (var room in _rooms.Values)
                {
                    room.CheckRoomSafety();
                }
            }
            catch (Exception e)
            {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");

                throw;
            }
        }

        /// <summary>
        /// Finds defined airtight rooms and their doors and vents.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws when doors or vents cannot be found.</exception>
        private void InitializeRooms()
        {
            _rooms = new Dictionary<string, AirtightRoom>();

            var doors = new List<IMyDoor>();
            _gtsHelper.GetBlocksOfType(doors);
            if (doors.Count == 0)
            {
                Echo($"Could not find doors.");
                throw new ArgumentNullException(nameof(doors));
            }

            // Assign (hopefully airtight) doors to correct rooms
            foreach (var door in doors)
            {
                // Match doors with outer door naming convention
                var outerDoorMatch = OuterDoorRegex.Match(door.CustomName);
                if (outerDoorMatch.Success)
                {
                    var roomName = outerDoorMatch.Groups[1].Value;
                    AddDoorToRoom(roomName, door, AirtightRoom.AddOuterDoor);
                }

                // Match doors with inner door naming convention
                var innerDoorMatch = InnerDoorRegex.Match(door.CustomName);
                if (innerDoorMatch.Success)
                {
                    var firstRoomName = innerDoorMatch.Groups[1].Value;
                    var secondRoomName = innerDoorMatch.Groups[2].Value;
                    AddDoorToRoom(firstRoomName, door, AirtightRoom.AddInnerDoor);
                    AddDoorToRoom(secondRoomName, door, AirtightRoom.AddInnerDoor);
                }
            }

            // Assign air vents to existing rooms
            var vents = new List<IMyAirVent>();
            _gtsHelper.GetBlocksOfType(vents);
            if (vents.Count == 0)
            {
                Echo($"Could not find vents.");
                throw new ArgumentNullException(nameof(vents));
            }

            // Find vents that match previously found rooms
            foreach (var vent in vents)
            {
                var ventMatch = VentRegex.Match(vent.CustomName);
                if (ventMatch.Success)
                {
                    var roomName = ventMatch.Groups[1].Value;
                    if (_rooms.ContainsKey(roomName))
                    {
                        AirtightRoom.AddAirVent(_rooms[roomName], vent);
                    }
                }
            }

            // Prune rooms that only have inner doors
            foreach (var roomName in _rooms.Keys.ToList())
            {
                if (_rooms[roomName].IsInnerRoom())
                {
                    _rooms.Remove(roomName);
                }
            }

            // Confirm that rooms are valid
            var roomsAreValid = true;
            foreach (var roomEntry in _rooms)
            {
                var room = roomEntry.Value;
                if (!room.IsValidRoom())
                {
                    Echo($"{roomEntry.Key} room is not valid. Please check if there is at least 1 outer door, inner door, and air vent.");
                    roomsAreValid = false;
                }
                else
                {
                    room.EchoRoomInfo();
                }
            }

            if (!roomsAreValid)
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Adds type of door (based on function provided) to provided room name.
        /// </summary>
        private void AddDoorToRoom(string roomName, IMyDoor door, Action<AirtightRoom, IMyDoor> addDoorFunc)
        {
            AirtightRoom room;
            if (!_rooms.TryGetValue(roomName, out room))
            {
                room = _rooms[roomName] = new AirtightRoom(roomName, this);
            }

            addDoorFunc(room, door);
        }
    }
}
