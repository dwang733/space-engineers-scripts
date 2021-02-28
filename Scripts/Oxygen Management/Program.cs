using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        /// The grid terminal system replacement
        /// </summary>
        private new readonly GridTerminalSystemV2 GridTerminalSystem;

        /// <summary>
        /// Matches inner door name with pattern "[Room 1]/[Room 2]".
        /// </summary>
        private readonly System.Text.RegularExpressions.Regex InnerDoorRegex = new System.Text.RegularExpressions.Regex(
            @"(\w+)\/(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches outer door name with pattern "[Room] Outer".
        /// </summary>
        private readonly System.Text.RegularExpressions.Regex OuterDoorRegex = new System.Text.RegularExpressions.Regex(
            @"(\w+)\s+Outer", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches air vent name with pattern "[Room] Vent".
        /// </summary>
        private readonly System.Text.RegularExpressions.Regex VentRegex = new System.Text.RegularExpressions.Regex(
            @"(\w+)\s+(?:Air\s*)?Vent", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// The switches that a user can pass via arguments that will activate oxygen emergency mode.
        /// </summary>
        private readonly string[] _userEmergencySwitches = new[] { "emergency", "recovery" };

        /// <summary>
        /// The command line utility class.
        /// </summary>
        private readonly MyCommandLine _myCommandLine = new MyCommandLine();

        /// <summary>
        /// The airtight rooms that this program manages.
        /// </summary>
        private Dictionary<string, AirtightRoom> _rooms = new Dictionary<string, AirtightRoom>();

        /// <summary>
        /// True if the user manually set the rooms to be in an oxygen emergency.
        /// </summary>
        private bool _inUserEmergency = false;

        public Program()
        {
            Echo("Script started!");

            GridTerminalSystem = new GridTerminalSystemV2(this);
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

                    if (!_myCommandLine.TryParse(argument))
                    {
                        Echo("No argument was given.");
                        if (_inUserEmergency)
                        {
                            Echo("User manually turned off oxygen emergency mode!");
                        }

                        _inUserEmergency = false;
                    }
                    else if (_userEmergencySwitches.Any(arg => _myCommandLine.Switch(arg)))
                    {
                        Echo("User manually activated oxygen emergency mode!");
                        _inUserEmergency = true;
                    }
                    else
                    {
                        Echo("Argument not recognized. Please use \"emergency\" or \"recovery\" flag.");
                        _inUserEmergency = false;
                    }

                    return;
                }

                // Commands from depressurized rooms need priority, so they should run last
                var pressurizedRooms = new List<AirtightRoom>();
                var depressurizedRooms = new List<AirtightRoom>();
                foreach (var room in _rooms.Values)
                {
                    room.UpdateState();
                    if (room.IsPressurized())
                    {
                        pressurizedRooms.Add(room);
                    }
                    else
                    {
                        depressurizedRooms.Add(room);
                    }
                }

                pressurizedRooms.ForEach(room => room.CheckRoomSafety(_inUserEmergency));
                depressurizedRooms.ForEach(room => room.CheckRoomSafety(_inUserEmergency));
            }
            catch (Exception e)
            {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");

                throw;
            }
        }

        /// <summary>
        /// Finds all airtight rooms and their doors and vents.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws when doors or vents cannot be found, or a room is invalid.</exception>
        private void InitializeRooms()
        {
            var newRoomNames = new HashSet<string>();

            // Clear doors and vents from existing rooms.
            foreach (var room in _rooms.Values)
            {
                room.ClearDoorsAndVents();
            }

            // Get all doors
            var doors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(doors);
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
                    var room = GetRoomByName(roomName);
                    var airtightDoor = new AirtightDoor(door, roomName);

                    room.AddOuterDoor(airtightDoor);
                    newRoomNames.Add(roomName);
                    continue;
                }

                // Match doors with inner door naming convention
                var innerDoorMatch = InnerDoorRegex.Match(door.CustomName);
                if (innerDoorMatch.Success)
                {
                    var firstRoomName = innerDoorMatch.Groups[1].Value;
                    var firstRoom = GetRoomByName(firstRoomName);
                    var secondRoomName = innerDoorMatch.Groups[2].Value;
                    var secondRoom = GetRoomByName(secondRoomName);

                    var airtightDoor = new AirtightDoor(door, firstRoomName, secondRoomName);
                    firstRoom.AddInnerDoor(airtightDoor);
                    secondRoom.AddInnerDoor(airtightDoor);

                    newRoomNames.Add(firstRoomName);
                    newRoomNames.Add(secondRoomName);
                }
            }

            // Get rid of rooms that no longer exist
            foreach (var roomName in _rooms.Keys.ToList())
            {
                if (!newRoomNames.Contains(roomName))
                {
                    _rooms.Remove(roomName);
                }
            }

            // Get all air vents
            var vents = new List<IMyAirVent>();
            GridTerminalSystem.GetBlocksOfType(vents);
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
                        _rooms[roomName].AddAirVent(vent);
                    }
                }
            }

            // Confirm that rooms are valid
            var roomsAreValid = true;
            foreach (var roomEntry in _rooms)
            {
                var room = roomEntry.Value;
                room.EchoRoomInfo();
                
                if (!room.IsValidRoom())
                {
                    Echo($"{roomEntry.Key} room is not valid. Please check if there is at least 1 inner door and 1 air vent.");
                    roomsAreValid = false;
                }
            }

            if (!roomsAreValid)
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Gets a room from the dictionary by its name.
        /// If the room does not exist yet, create a new one.
        /// </summary>
        private AirtightRoom GetRoomByName(string roomName)
        {
            AirtightRoom room;
            if (!_rooms.TryGetValue(roomName, out room))
            {
                room = _rooms[roomName] = new AirtightRoom(roomName, this);
            }

            return room;
        }
    }
}
