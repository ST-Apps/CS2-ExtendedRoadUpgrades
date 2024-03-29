namespace ExtendedRoadUpgrades.Utils
{
    using System;
    using System.Reflection;
    using HarmonyLib;

    /// <summary>
    /// Copied over from https://github.com/BepInEx/HarmonyX/blob/master/Harmony/Public/Harmony.cs
    /// Seems like HarmonyX has issues with PDX Mods at the moment so this is a quick compatibility workaround.
    /// </summary>
    // Disabling checks as we don't want to edit this file.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:Element return value should be documented", Justification = "File copied from other source, won't change.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1629:Documentation text should end with a period", Justification = "File copied from other source, won't change.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1503:Braces should not be omitted", Justification = "File copied from other source, won't change.")]
    internal static class HarmonyUtils
    {
        private static int _autoGuidCounter = 100;

        /// <summary>Creates a new Harmony instance and applies all patches specified in the type</summary>
        /// <param name="type">The type to scan for patches.</param>
        /// <param name="harmonyInstanceId">ID of the Harmony instance which will be created. Specify the ID if other plugins may want to interact with your patches.</param>
        ///
        public static Harmony CreateAndPatchAll(Type type, string harmonyInstanceId = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var harmony = new Harmony(harmonyInstanceId ?? $"harmony-auto-{System.Threading.Interlocked.Increment(ref _autoGuidCounter)}-{type.Assembly.GetName().Name}-{type.FullName}");
            harmony.PatchAll(type.Assembly);
            return harmony;
        }

        /// <summary>Applies all patches specified in the assembly</summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="harmonyInstanceId">ID of the Harmony instance which will be created. Specify the ID if other plugins may want to interact with your patches.</param>
        ///
        public static Harmony CreateAndPatchAll(Assembly assembly, string harmonyInstanceId = null)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var harmony = new Harmony(harmonyInstanceId ?? $"harmony-auto-{System.Threading.Interlocked.Increment(ref _autoGuidCounter)}-{assembly.GetName().Name}");
            harmony.PatchAll(assembly);
            return harmony;
        }
    }
}
