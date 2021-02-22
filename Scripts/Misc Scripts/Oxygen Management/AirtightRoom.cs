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
    partial class Program
    {
        /// <summary>
        /// Defines an airtight room with outer doors (to vacuum/space), inner doors (to pressurized area), and air vents.
        /// </summary>
        public class AirtightRoom
        {
            private readonly string _roomName;
            private readonly Program _program;

            private readonly List<IMyDoor> _innerDoors = new List<IMyDoor>();
            private readonly List<IMyDoor> _outerDoors = new List<IMyDoor>();
            private readonly List<IMyAirVent> _airVents = new List<IMyAirVent>();

            /// <summary>
            /// True if any inner door is not fully closed, false otherwise.
            /// </summary>
            bool _innerDoorOpen = false;

            /// <summary>
            /// True if any outer door is not fully closed, false otherwise.
            /// </summary>
            bool _outerDoorOpen = false;

            /// <summary>
            /// True if all vents are set to pressurized, false otherwise.
            /// </summary>
            bool _pressurized = false;

            /// <summary>
            /// The average oxygen level of the room, as a percentage from 0.0-1.0.
            /// </summary>
            float _oxygenLevel = 0;

            /// <summary>
            /// True if the oxygen level needs to be monitored when the room is in depressurize mode.
            /// </summary>
            private bool _monitorOxygenLevel = false;

            /// <summary>
            /// Gets the oxygen level of the room during the previous tick.
            /// </summary>
            private float _prevOxygenLevel = 1;

            /// <summary>
            /// Gets whether the vents were pressurizing during the previous tick.
            /// </summary>
            private bool? _prevPressurized = null;

            /// <summary>
            /// True if room is in an oxygen emergency (e.g. oxygen is leaving the room into space), false otherwise
            /// </summary>
            private bool _inEmergency = false;

            /// <summary>
            /// Gets whether the vents were pressurizing before the emergency.
            /// </summary>
            private bool _pressurizedBeforeEmergency = false;

            public AirtightRoom(string roomName, Program program)
            {
                _roomName = roomName;
                _program = program;
            }

            /// <summary>
            /// Checks the room to ensure that no oxygen is lost.
            /// </summary>
            public void CheckRoomSafety()
            {
                UpdateRoomVariables();

                // If any doors are open while trying to change air vent depressurize status, revert back for safety.
                if (!_prevPressurized.HasValue)
                {
                    _prevPressurized = _pressurized;
                }
                else if (_prevPressurized.Value != _pressurized && (_innerDoorOpen || _outerDoorOpen))
                {
                    _airVents.ForEach(vent => vent.Depressurize = !_prevPressurized.Value);
                    _monitorOxygenLevel = true;
                    return;
                }

                // Check the room status
                var inEmergencyNow = _pressurized ? CheckPressurizedRoom() : CheckDepressurizedRoom();

                // If emergency is over, revert air vents to previous status
                if (_inEmergency && !inEmergencyNow)
                {
                    _program.Echo($"Emergency for {_roomName} is over");
                    _airVents.ForEach(vent => vent.Depressurize = !_pressurizedBeforeEmergency);
                }

                _inEmergency = inEmergencyNow;
                _prevPressurized = _pressurized;
            }

            public int InnerDoorsCount()
            {
                return _innerDoors.Count;
            }

            public int OuterDoorsCount()
            {
                return _outerDoors.Count;
            }

            public int AirVentsCount()
            {
                return _airVents.Count;
            }

            public bool IsInnerRoom()
            {
                return _outerDoors.Count == 0;
            }

            public bool IsValidRoom()
            {
                return _innerDoors.Count > 0 && _airVents.Count > 0;
            }

            public static void AddInnerDoor(AirtightRoom room, IMyDoor door)
            {
                room._innerDoors.Add(door);
            }

            public static void AddOuterDoor(AirtightRoom room, IMyDoor door)
            {
                room._outerDoors.Add(door);
            }

            public static void AddAirVent(AirtightRoom room, IMyAirVent vent)
            {
                room._airVents.Add(vent);
            }

            /// <summary>
            /// Checks the status of doors in a pressurized room.
            /// </summary>
            /// <returns>true if the room is in an emergency, false otherwise.</returns>
            private bool CheckPressurizedRoom()
            {
                // Handle emergency scenario where outer doors are open
                if (_outerDoorOpen)
                {
                    _program.Echo($"Oxygen emergency in {_roomName}: outer door open in pressurized room!");
                    HandleOxygenEmergency();
                    return true;
                }

                // Make sure outer doors are disabled
                _outerDoors.ForEach(door => door.Enabled = false);

                // Make sure inner doors are enabled
                _innerDoors.ForEach(door => door.Enabled = true);

                _monitorOxygenLevel = true;
                return false;
            }

            /// <summary>
            /// Checks the status of doors in a depressurized room.
            /// </summary>
            /// <returns>true if the room is not in an emergency, false otherwise.</returns>
            private bool CheckDepressurizedRoom()
            {
                // Handle emergency scenario where both sets of doors are open
                if (_innerDoorOpen && _outerDoorOpen)
                {
                    _program.Echo($"Oxygen emergency in {_roomName}: outer and inner doors open in depressurized room!");
                    HandleOxygenEmergency();
                    return true;
                }

                // Make sure all vents are set to depressurize
                _airVents.ForEach(vent => vent.Depressurize = true);

                // Disable outer doors if inner doors are open
                if (_innerDoorOpen)
                {
                    _outerDoors.ForEach(door => door.Enabled = false);
                    _monitorOxygenLevel = true;
                    return false;
                }

                // Disable inner doors if outer doors are open
                if (_outerDoorOpen)
                {
                    _innerDoors.ForEach(door => door.Enabled = false);
                    _monitorOxygenLevel = true;
                    return false;
                }

                // Checks if both sets of doors are closed
                if (!_innerDoorOpen && !_outerDoorOpen)
                {
                    // If both sets of doors are locked, re-enable them once room is depressurized as much as possible
                    var oxygenLevelDiff = _prevOxygenLevel - _oxygenLevel;
                    if (_monitorOxygenLevel && (_oxygenLevel == 0 || Math.Abs(oxygenLevelDiff) < 0.0005))
                    {
                        _innerDoors.ForEach(door => door.Enabled = true);
                        _outerDoors.ForEach(door => door.Enabled = true);
                        _monitorOxygenLevel = false;
                    }
                    // If room has oxygen, lock outer doors until fully depressurized
                    else if (_oxygenLevel > 0)
                    {
                        //_innerDoors.ForEach(door => door.Enabled = false);
                        _innerDoors.ForEach(door => door.Enabled = true);
                        _outerDoors.ForEach(door => door.Enabled = false);
                        _monitorOxygenLevel = true;
                    }
                }

                _prevOxygenLevel = _oxygenLevel;
                return false;
            }

            /// <summary>
            /// Updates the state variables for doors and air vents.
            /// </summary>
            private void UpdateRoomVariables()
            {
                _innerDoorOpen = _innerDoors.Exists(door => door.Status != DoorStatus.Closed);
                _outerDoorOpen = _outerDoors.Exists(door => door.Status != DoorStatus.Closed);
                _pressurized = _airVents.All(vent => !vent.Depressurize);
                _oxygenLevel = _airVents.Sum(vent => vent.GetOxygenLevel()) / _airVents.Count;
            }

            /// <summary>
            /// Commands doors and air vents to reduce severity of oxygen emergency (i.e. oxygen escaping into space).
            /// </summary>
            private void HandleOxygenEmergency()
            {
                // Close all doors and set all vents to depressurize
                _outerDoors.ForEach(door =>
                {
                    door.Enabled = true;
                    door.CloseDoor();
                });
                _innerDoors.ForEach(door =>
                {
                    door.Enabled = true;
                    door.CloseDoor();
                });
                _airVents.ForEach(vent => vent.Depressurize = true);

                // If this is the very start of the emergency, mark the previous air vents' status
                if (!_inEmergency)
                {
                    _pressurizedBeforeEmergency = _pressurized;
                }

                _inEmergency = true;
                _monitorOxygenLevel = true;
            }
        }
    }
}
