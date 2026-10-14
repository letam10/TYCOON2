using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tycoon
{
    // Sao chép DTO trước khi giao cho worker; không serialize hay giữ tham chiếu world sống.
    internal static class CheckpointSnapshot
    {
        static readonly Func<object, object> Clone = (Func<object, object>)typeof(object)
            .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Func<object, object>));
        static readonly ConcurrentDictionary<Type, FieldInfo[]> References = new();

        internal static T Copy<T>(T value) => (T)CopyValue(value);

        static object CopyValue(object value)
        {
            if (value == null) return null;
            var type = value.GetType();
            if (type.IsValueType || type == typeof(string)) return value;
            // String bất biến; sao chép list trực tiếp, tránh reflection cho hàng nghìn event rỗng.
            if (type == typeof(List<string>)) return new List<string>((List<string>)value);
            if (type == typeof(TransactionReceipt)) return CopyReceipt((TransactionReceipt)value);
            if (type == typeof(TransactionEvent)) return CopyEvent((TransactionEvent)value);
            if (type == typeof(List<TransactionReceipt>)) return CopyReceipts((List<TransactionReceipt>)value);
            if (type == typeof(List<TransactionEvent>)) return CopyEvents((List<TransactionEvent>)value);
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
            var copy = Clone(value);
            foreach (var field in References.GetOrAdd(type, ReferenceFields))
                field.SetValue(copy, CopyValue(field.GetValue(value)));
            return copy;
        }

        // DTO sealed chỉ có trường bất biến, trừ consumers được sao chép riêng; test khóa schema này.
        static TransactionReceipt CopyReceipt(TransactionReceipt source) => source == null
            ? null
            : (TransactionReceipt)Clone(source);

        static TransactionEvent CopyEvent(TransactionEvent source)
        {
            if (source == null) return null;
            var copy = (TransactionEvent)Clone(source);
            copy.consumers = source.consumers == null ? null : new List<string>(source.consumers);
            return copy;
        }

        static List<TransactionReceipt> CopyReceipts(List<TransactionReceipt> source)
        {
            var copy = new List<TransactionReceipt>(source.Count);
            for (int index = 0; index < source.Count; index++)
                copy.Add(CopyReceipt(source[index]));
            return copy;
        }

        static List<TransactionEvent> CopyEvents(List<TransactionEvent> source)
        {
            var copy = new List<TransactionEvent>(source.Count);
            for (int index = 0; index < source.Count; index++)
                copy.Add(CopyEvent(source[index]));
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

        internal TransactionState CaptureDeferredCheckpoint(out CheckpointHistoryCapture history)
        {
            lock (gate)
            {
                // World và history cùng revision; worker chỉ nhận dữ liệu đã tách khỏi state sống.
                var withoutHistory = state.ShallowCopy();
                withoutHistory.receipts = null;
                withoutHistory.outbox = null;
                var owned = CheckpointSnapshot.Copy(withoutHistory);
                history = CheckpointHistoryCapture.Create(state.receipts, state.outbox);
                return owned;
            }
        }
    }
}
