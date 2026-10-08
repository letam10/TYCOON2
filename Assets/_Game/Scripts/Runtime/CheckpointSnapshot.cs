using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Tycoon
{
    // Sao chép DTO trước khi giao cho worker; không serialize hay giữ tham chiếu world sống.
    internal static class CheckpointSnapshot
    {
        static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly ConcurrentDictionary<Type, FieldInfo[]> References = new();

        internal static T Copy<T>(T value) => (T)CopyValue(value);

        static object CopyValue(object value)
        {
            if (value == null) return null;
            var type = value.GetType();
            if (type.IsValueType || type == typeof(string)) return value;
            if (value is Array sourceArray)
            {
                var array = (Array)sourceArray.Clone();
                for (int index = 0; index < array.Length; index++)
                    array.SetValue(CopyValue(sourceArray.GetValue(index)), index);
                return array;
            }
            if (value is IList source)
            {
                var list = (IList)Activator.CreateInstance(type, source.Count);
                foreach (var item in source) list.Add(CopyValue(item));
                return list;
            }
            var copy = Clone.Invoke(value, null);
            foreach (var field in References.GetOrAdd(type, ReferenceFields))
                field.SetValue(copy, CopyValue(field.GetValue(value)));
            return copy;
        }

        static FieldInfo[] ReferenceFields(Type type) => type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(field => !field.FieldType.IsValueType && field.FieldType != typeof(string))
            .ToArray();
    }

    public sealed partial class TransactionCore
    {
        internal TransactionState CaptureCheckpoint()
        {
            lock (gate) return CheckpointSnapshot.Copy(state);
        }
    }
}
