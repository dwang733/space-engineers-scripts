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
        /// Defines an airtight room with air vents, inner doors (to pressurized area), and optional outer doors (to vacuum/space)
        /// </summary>
        public class AirtightRoom
        {
            private readonly string _roomName;
            private readonly Program _program;

            private List<AirtightDoor> _innerDoors = new List<AirtightDoor>();
            private List<AirtightDoor> _outerDoors = new List<AirtightDoor>();
            private List<IMyAirVent> _airVents = new List<IMyAirVent>();

            /// <summary>
            /// True if any inner door is not fully closed, false otherwise.
            /// Updated as a state variable.
            /// </summary>
            bool _innerDoorOpen = false;

            /// <summary>
            /// True if any outer door is not fully closed, false otherwise.
            /// Updated as a state variable.
            /// </summary>
            bool _outerDoorOpen = false;

            /// <summary>
            /// True if all vents are set to pressurized, false otherwise.
            /// Updated as a state variable.
            /// </summary>
            bool _pressurized = false;

            /// <summary>
            /// The average oxygen level of the room, as a percentage from 0.0-1.0.
            /// Updated as a state variable.
            /// </summary>
            float _oxygenLevel = 0;

            /// <summary>
            /// True if the oxygen level needs to be checked when the room enters depressurize mode, false otherwise.
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
            /// Updates the state variables for doors and air vents.
            /// </summary>
            public void UpdateState()
            {
                _innerDoorOpen = _innerDoors.Exists(door => door.IsOpen());
                _outerDoorOpen = _outerDoors.Exists(door => door.IsOpen());
                _pressurized = _airVents.All(vent => !vent.Depressurize);
                _oxygenLevel = _airVents.Sum(vent => vent.GetOxygenLevel()) / _airVents.Count;
            }

            /// <summary>
            /// Checks the room to ensure that no oxygen is lost.
            /// </summary>
            /// <param name="userEmergencyMode">True if the user manually set the grid to oxygen emergency mode, false otherwise.</param>
            public void CheckRoomSafety(bool userEmergencyMode)
            {
                // Check if user activated emergency mode manually
                if (userEmergencyMode)
                {
                    _program.Echo($"Oxygen emergency in {_roomName}: manually activated by user");
                    HandleOxygenEmergency();
                    _monitorOxygenLevel = true;
                    return;
                }

                // Check if room is no longer airtight
                var cannotPressurize = _airVents.Any(vent => !vent.CanPressurize);
                if (cannotPressurize && !_outerDoorOpen)
                {
                    _program.Echo($"Oxygen emergency in {_roomName}: room is no longer airtight!");
                    HandleOxygenEmergency();
                    _monitorOxygenLevel = true;
                    return;
                }

                // If any doors are open while trying to change air vent depressurize status, revert back for safety
                if (!_prevPressurized.HasValue)
                {
                    _prevPressurized = _pressurized;
                    _monitorOxygenLevel = true;
                }
                else if (_prevPressurized.Value != _pressurized && (_innerDoorOpen || _outerDoorOpen))
                {
                    _airVents.ForEach(vent => vent.Depressurize = !_prevPressurized.Value);
                    _monitorOxygenLevel = true;
                    return;
                }

                // Check the room status
                var inEmergencyNow = _pressurized ? CheckPressurizedRoom() : CheckDepressurizedRoom();

                // If emergency is over, revert air vents to previous status and disable room's emergency status on door
                if (_inEmergency && (!inEmergencyNow || !userEmergencyMode))
                {
                    _program.Echo($"Emergency for {_roomName} is over");
                    _airVents.ForEach(vent => vent.Depressurize = !_pressurizedBeforeEmergency);

                    // Update state of all doors
                    _innerDoors.ForEach(door => door.UpdateEmergencyState(_roomName, false));
                    _outerDoors.ForEach(door => door.UpdateEmergencyState(_roomName, false));
                }

                // Update remaining state variables
                _inEmergency = inEmergencyNow;
                _prevPressurized = _pressurized;
            }

            public void AddInnerDoor(AirtightDoor door)
            {
                _innerDoors.Add(door);
            }

            public void AddOuterDoor(AirtightDoor door)
            {
                _outerDoors.Add(door);
            }

            public void AddAirVent(IMyAirVent vent)
            {
                _airVents.Add(vent);
            }

            public void ClearDoorsAndVents()
            {
                _innerDoors = new List<AirtightDoor>();
                _outerDoors = new List<AirtightDoor>();
                _airVents = new List<IMyAirVent>();
            }

            public void EchoRoomInfo()
            {
                _program.Echo($"Room [{_roomName}] has {_innerDoors.Count} inner doors, {_outerDoors.Count} outer doors, and {_airVents.Count} air vents.");
            }

            public bool IsValidRoom()
            {
                return _innerDoors.Count > 0 && _airVents.Count > 0;
            }

            public bool IsPressurized()
            {
                return _pressurized;
            }

            /// <summary>
            /// Checks the status of a pressurized room.
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
                _outerDoors.ForEach(door => door.Disable());

                // Make sure inner doors are enabled
                _innerDoors.ForEach(door => door.Enable());

                _monitorOxygenLevel = true;
                return false;
            }

            /// <summary>
            /// Checks the status of a depressurized room.
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
                    _program.Echo($"Inner doors are open in {_roomName} - disabling outer doors");
                    _outerDoors.ForEach(door => door.Disable());
                    _monitorOxygenLevel = true;
                    return false;
                }

                // Disable inner doors if outer doors are open
                if (_outerDoorOpen)
                {
                    _program.Echo($"Outer doors are open in {_roomName} - disabling inner doors");
                    _innerDoors.ForEach(door => door.Disable());
                    _monitorOxygenLevel = true;
                    return false;
                }

                // Checks if both sets of doors are closed
                if (!_innerDoorOpen && !_outerDoorOpen)
                {
                    // If oxygen level needs to be checked, re-enable the doors once room is depressurized as much as possible
                    var oxygenLevelDiff = _prevOxygenLevel - _oxygenLevel;
                    if (_monitorOxygenLevel && (_oxygenLevel == 0 || Math.Abs(oxygenLevelDiff) < 0.0005))
                    {
                        _innerDoors.ForEach(door => door.Enable());
                        _outerDoors.ForEach(door => door.Enable());
                        _monitorOxygenLevel = false;
                    }
                    // If room has oxygen, lock outer doors until fully depressurized
                    else if (_oxygenLevel > 0)
                    {
                        _program.Echo($"Locking all doors in {_roomName} until depressurized");
                        _innerDoors.ForEach(door => door.Enable());
                        _outerDoors.ForEach(door => door.Disable());
                        _monitorOxygenLevel = true;
                    }
                }

                _prevOxygenLevel = _oxygenLevel;
                return false;
            }

            /// <summary>
            /// Commands doors and air vents to reduce severity of oxygen emergency (i.e. oxygen escaping into space).
            /// </summary>
            private void HandleOxygenEmergency()
            {
                // Set all vents to depressurize
                _airVents.ForEach(vent => vent.Depressurize = true);

                // Update state of all doors
                _innerDoors.ForEach(door => door.UpdateEmergencyState(_roomName, true));
                _outerDoors.ForEach(door => door.UpdateEmergencyState(_roomName, true));

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
