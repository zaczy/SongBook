using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Zaczy.SongBook.Extensions;

public static class ContentCopier
{
    public static object? ShallowCopyTo(this object obj, object target, List<string>? exceptionsList=null)
    {
        if (obj == null)
            return null;

        var sourceType = obj.GetType();

        // If target is null create new instance of source type
        Type targetType;
        object? resultTarget = target;
        if (target == null)
        {
            targetType = sourceType;
            try
            {
                resultTarget = Activator.CreateInstance(targetType)
                    ?? throw new InvalidOperationException($"Unable to create instance of type {targetType.FullName}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of type {targetType.FullName}", ex);
            }
        }
        else
        {
            targetType = target.GetType();
        }

        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var sProp in sourceProperties)
        {
            // skip indexers and non-readable props
            if (!sProp.CanRead || sProp.GetIndexParameters().Length > 0)
                continue;

            if (exceptionsList != null && exceptionsList.Contains(sProp.Name))
                continue;

            var tProp = targetType.GetProperty(sProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (tProp == null)
                continue;

            // skip indexers and non-writable props on target
            if (!tProp.CanWrite || tProp.GetIndexParameters().Length > 0)
                continue;

            // try direct assignment when types are compatible
            if (tProp.PropertyType.IsAssignableFrom(sProp.PropertyType))
            {
                var value = sProp.GetValue(obj);
                tProp.SetValue(resultTarget, value);
                continue;
            }

            // attempt simple conversion for primitive/IConvertible types
            var sourceValue = sProp.GetValue(obj);
            if (sourceValue == null)
            {
                tProp.SetValue(resultTarget, null);
                continue;
            }

            try
            {
                var converted = Convert.ChangeType(sourceValue, tProp.PropertyType);
                tProp.SetValue(resultTarget, converted);
            }
            catch
            {
                // incompatible — skip silently (shallow copy)
            }
        }

        return resultTarget;
    }
}
