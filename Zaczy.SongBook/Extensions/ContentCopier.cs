using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Zaczy.SongBook.Extensions;

public static class ContentCopier
{


    public static bool HasSignigicantDifferences(this object obj, object other, List<string>? exceptionsList = null, List<SongDiffSpecification>? differenciesFound=null)
    {
        if (obj == null || other == null)
            return false;
        var type = obj.GetType();
        var otherType = other.GetType();

        //if (type != other.GetType())
        //    return true;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            // skip indexers and non-readable props
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;
            if (exceptionsList != null && exceptionsList.Contains(prop.Name))
                continue;
        
            var otherProp = otherType.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
            if (otherProp == null)
            {
                continue;
            }

            var value1 = prop.GetValue(obj);
            var value2 = otherProp?.GetValue(other);
            if (!Equals(value1, value2))
            {
                if(prop.PropertyType == typeof(string))
                {
                    string s1 = (value1 as string)?.Trim() ?? string.Empty;
                    string s2 = (value2 as string)?.Trim() ?? string.Empty;
                    if (s1.Equals(s2, StringComparison.Ordinal))
                        continue; // ignore whitespace differences in strings
                }

                System.Diagnostics.Debug.WriteLine($"HasSignigicantDifferences: Property {prop.Name} differs: {value1} != {value2}");

                if (differenciesFound != null)
                    differenciesFound.Add(new SongDiffSpecification(prop.Name, value1?.ToString() ?? String.Empty, value2?.ToString() ?? String.Empty));
                else
                    return true;
            }
        }

        if (differenciesFound != null)
            return differenciesFound.Count > 0;
        else
            return false;
    }

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
                var currentValue = tProp.GetValue(resultTarget);
                if (!Equals(value, currentValue))
                {
                    System.Diagnostics.Debug.WriteLine($"ShallowCopyTo: Property {sProp.Name} changed from {currentValue} to {value}");
                    tProp.SetValue(resultTarget, value);
                }
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
