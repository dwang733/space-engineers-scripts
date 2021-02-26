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
    /// <summary>
    /// Extension methods for <see cref="MyIni"/>.
    /// </summary>
    static class MyIniExtensions
    {
        /// <summary>
        /// Get the value of a required configuration key.
        /// </summary>
        /// <typeparam name="T">The type of value to be returned.</typeparam>
        /// <param name="myIni">The <see cref="MyIni"/> object to use.</param>
        /// <param name="section">The section to get.</param>
        /// <param name="name">The key name to get.</param>
        /// <returns>The value of the configuration key as the specified type <typeparamref name="T"/></returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the configuration key can't be found or the value cannot be converted into the type <typeparamref name="T"/>
        /// </exception>
        public static T GetRequired<T>(this MyIni myIni, string section, string name)
        {
            var iniValue = myIni.Get(section, name);
            if (iniValue.Equals(MyIniValue.EMPTY))
            {
                throw new ArgumentException($"Could not find INI configuration key {section}/{name}.");
            }

            return iniValue.ToType<T>();
        }

        /// <summary>
        /// Get the value of a required configuration key as a list of values.
        /// </summary>
        /// <typeparam name="T">The type of value to be returned.</typeparam>
        /// <param name="myIni">The <see cref="MyIni"/> object to use.</param>
        /// <param name="section">The section to get.</param>
        /// <param name="name">The key name to get.</param>
        /// <returns>The value of the configuration key as a list of the specified type <typeparamref name="T"/></returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the configuration key can't be found, the list is empty, or a value cannot be converted into the type <typeparamref name="T"/>
        /// </exception>
        public static List<T> GetRequiredAsList<T>(this MyIni myIni, string section, string name)
        {
            var iniValue = myIni.Get(section, name);
            if (iniValue.Equals(MyIniValue.EMPTY))
            {
                throw new ArgumentException($"Could not find INI configuration key {section}/{name}.");
            }

            var valuesAsStr = iniValue.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (valuesAsStr.Count() == 0)
            {
                throw new ArgumentException($"Could not parse INI configuration key {section}/{name} as CSV.");
            }

            var values = valuesAsStr.Select(str => (T)Convert.ChangeType(str, typeof(T))).ToList();
            return values;
        }

        /// <summary>
        /// Get the value of an optional configuration key, or the specified default if the value doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of value to be returned.</typeparam>
        /// <param name="myIni">The <see cref="MyIni"/> object to use.</param>
        /// <param name="section">The section to get.</param>
        /// <param name="name">The key name to get.</param>
        /// <returns>The value of the configuration key as the specified type <typeparamref name="T"/>, or <paramref name="defaultValue"/> if the key doesn't have a value.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the configuration key can't be found or the value cannot be converted into the type <typeparamref name="T"/>/
        /// </exception>
        public static T GetOptional<T>(this MyIni myIni, string section, string name, T defaultValue = default(T))
        {
            var iniValue = myIni.Get(section, name);
            if (iniValue.Equals(MyIniValue.EMPTY))
            {
                throw new ArgumentException($"Could not find INI configuration key {section}/{name}.");
            }

            if (string.IsNullOrWhiteSpace(iniValue.ToString()))
            {
                return defaultValue;
            }

            return iniValue.ToType<T>();
        }

        /// <summary>
        /// Get the value of an optional configuration key as a list of values, or null if the value doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of value to be returned.</typeparam>
        /// <param name="myIni">The <see cref="MyIni"/> object to use.</param>
        /// <param name="section">The section to get.</param>
        /// <param name="name">The key name to get.</param>
        /// <returns>The value of the configuration key as a list of the specified type <typeparamref name="T"/>, or null if the key doesn't have a value.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the configuration key can't be found or a value cannot be converted into the type <typeparamref name="T"/>
        /// </exception>
        public static List<T> GetOptionalAsList<T>(this MyIni myIni, string section, string name)
        {
            var iniValue = myIni.Get(section, name);
            if (iniValue.Equals(MyIniValue.EMPTY))
            {
                throw new ArgumentException($"Could not find INI configuration key {section}/{name}.");
            }

            var valuesAsStr = iniValue.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (valuesAsStr.Count() == 0)
            {
                return null;
            }

            var values = valuesAsStr.Select(str => (T)Convert.ChangeType(str, typeof(T))).ToList();
            return values;
        }

        /// <summary>
        /// Converts a <see cref="MyIniValue"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">The type of value to be returned.</typeparam>
        /// <param name="iniValue">The <see cref="MyIniValue"/> to convert.</param>
        /// <returns>The INI value as type <typeparamref name="T"/> if the conversion is successful.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="MyIniValue"/> could not be converted to type <typeparamref name="T"/>.</exception>
        public static T ToType<T>(this MyIniValue iniValue)
        {
            try
            {
                var valueAsStr = iniValue.ToString();
                return (T)Convert.ChangeType(valueAsStr, typeof(T));
            }
            catch
            {
                var iniKey = iniValue.Key;
                throw new ArgumentException($"Could not convert INI configuration key {iniKey.Section}/{iniKey.Name} to type {typeof(T)}.");
            }
        }
    }
}
