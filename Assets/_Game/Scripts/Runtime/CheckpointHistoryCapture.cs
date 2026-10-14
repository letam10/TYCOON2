using System;
using System.Collections.Generic;

namespace Tycoon
{
    internal sealed class CheckpointHistoryCapture
    {
        readonly ReceiptValue[] receipts;
        readonly EventValue[] events;
        readonly string[] consumers;

        CheckpointHistoryCapture(ReceiptValue[] receipts, EventValue[] events, string[] consumers)
        {
            this.receipts = receipts;
            this.events = events;
            this.consumers = consumers;
        }

        internal static CheckpointHistoryCapture Create(
            List<TransactionReceipt> receipts, List<TransactionEvent> events)
        {
            var receiptValues = receipts == null ? null : new ReceiptValue[receipts.Count];
            if (receipts != null)
            {
                for (int index = 0; index < receipts.Count; index++)
                    receiptValues[index] = new ReceiptValue(receipts[index]);
            }

            var eventValues = events == null ? null : new EventValue[events.Count];
            int consumerCount = 0;
            if (events != null)
            {
                for (int index = 0; index < events.Count; index++)
                    consumerCount = checked(consumerCount + (events[index]?.consumers?.Count ?? 0));
            }

            // Caller giữ lock; hai lượt đọc luôn thuộc cùng một revision.
            var consumerValues = consumerCount == 0 ? Array.Empty<string>() : new string[consumerCount];
            int offset = 0;
            if (events != null)
            {
                for (int index = 0; index < events.Count; index++)
                {
                    var source = events[index];
                    eventValues[index] = new EventValue(source, offset);
                    if (source?.consumers == null)
                        continue;
                    source.consumers.CopyTo(consumerValues, offset);
                    offset += source.consumers.Count;
                }
            }
            return new CheckpointHistoryCapture(receiptValues, eventValues, consumerValues);
        }

        internal void Materialize(TransactionState ownedState)
        {
            // Chỉ worker tạo DTO/list; capture không giữ tham chiếu mutable của nguồn.
            ownedState.receipts = receipts == null ? null : new List<TransactionReceipt>(receipts.Length);
            if (receipts != null)
            {
                foreach (var receipt in receipts)
                    ownedState.receipts.Add(receipt.Materialize());
            }
            ownedState.outbox = events == null ? null : new List<TransactionEvent>(events.Length);
            if (events != null)
            {
                foreach (var transactionEvent in events)
                    ownedState.outbox.Add(transactionEvent.Materialize(consumers));
            }
        }

        readonly struct ReceiptValue
        {
            readonly bool exists;
            readonly string id;
            readonly string key;
            readonly string fingerprint;
            readonly string effectId;
            readonly string effectFingerprint;
            readonly string eventId;
            readonly long revision;
            readonly int amount;
            readonly long cashAmount;

            internal ReceiptValue(TransactionReceipt source)
            {
                exists = source != null;
                id = source?.id;
                key = source?.key;
                fingerprint = source?.fingerprint;
                effectId = source?.effectId;
                effectFingerprint = source?.effectFingerprint;
                eventId = source?.eventId;
                revision = source?.revision ?? 0;
                amount = source?.amount ?? 0;
                cashAmount = source?.cashAmount ?? 0;
            }

            internal TransactionReceipt Materialize()
            {
                if (!exists)
                    return null;
                return new TransactionReceipt
                {
                    id = id,
                    key = key,
                    fingerprint = fingerprint,
                    effectId = effectId,
                    effectFingerprint = effectFingerprint,
                    eventId = eventId,
                    revision = revision,
                    amount = amount,
                    cashAmount = cashAmount
                };
            }
        }

        readonly struct EventValue
        {
            readonly bool exists;
            readonly string id;
            readonly string receiptId;
            readonly string effectId;
            readonly string kind;
            readonly long revision;
            readonly bool nullConsumers;
            readonly int consumerOffset;
            readonly int consumerCount;

            internal EventValue(TransactionEvent source, int offset)
            {
                exists = source != null;
                id = source?.id;
                receiptId = source?.receiptId;
                effectId = source?.effectId;
                kind = source?.kind;
                revision = source?.revision ?? 0;
                nullConsumers = source?.consumers == null;
                consumerOffset = offset;
                consumerCount = source?.consumers?.Count ?? 0;
            }

            internal TransactionEvent Materialize(string[] capturedConsumers)
            {
                if (!exists)
                    return null;
                var result = new TransactionEvent
                {
                    id = id,
                    receiptId = receiptId,
                    effectId = effectId,
                    kind = kind,
                    revision = revision,
                    consumers = nullConsumers ? null : new List<string>(consumerCount)
                };
                for (int index = 0; index < consumerCount; index++)
                    result.consumers.Add(capturedConsumers[consumerOffset + index]);
                return result;
            }
        }
    }
}
