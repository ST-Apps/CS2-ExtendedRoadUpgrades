using Game.Prefabs;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ExtendedRoadUpgrades.Extensions
{

    /// <summary>
    /// This class contains a bruteforce method to update a <see cref="PrefabBase.thumbnailUrl"/>
    /// after the <see cref="PrefabBase"/> name is set.
    /// This is needed because <see cref="PrefabBase.thumbnailUrl"/> is set only during the <see cref="PrefabBase.OnEnable"/>
    /// method, which happens before being able to set a name.
    /// </summary>
    internal static class PrefabBaseExtensions
    {
        // ChatGPT wrote this, I take no responsibility for it
        static void ModifyReadonlyProperty(PrefabBase instance, string value, string propertyName)
        {
            PropertyInfo propertyInfo = typeof(PrefabBase).GetProperty(propertyName);

            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                // Create a property setter delegate using expression trees
                Action<PrefabBase, string> setMethod = CreatePropertySetter<PrefabBase, string>(propertyInfo);

                // Invoke the setter delegate to set the value
                setMethod(instance, value);
            }
        }

        // ChatGPT wrote this, I take no responsibility for it
        static Action<T, TValue> CreatePropertySetter<T, TValue>(PropertyInfo propertyInfo)
        {
            ParameterExpression instance = Expression.Parameter(typeof(T), "instance");
            ParameterExpression value = Expression.Parameter(typeof(TValue), "value");

            MemberExpression property = Expression.Property(instance, propertyInfo);
            BinaryExpression assign = Expression.Assign(property, value);

            return Expression.Lambda<Action<T, TValue>>(assign, instance, value).Compile();
        }

        public static void UpdateThumbnailUrl(this PrefabBase prefabBase)
        {
            var thumbnailUrl = "thumbnail://ThumbnailCamera/" + Uri.EscapeDataString(prefabBase.GetType().Name) + "/" + Uri.EscapeDataString(prefabBase.name);
            Plugin.Logger.LogInfo($"Updating thumbnailUrl for {prefabBase.name} from {prefabBase.thumbnailUrl} to {thumbnailUrl}...");
            ModifyReadonlyProperty(prefabBase, thumbnailUrl, "thumbnailUrl");
            Plugin.Logger.LogDebug($"thumbnailUrl for {prefabBase.name} updated to {prefabBase.thumbnailUrl}...");
        }
    }
}
