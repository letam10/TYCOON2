using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Tycoon
{
    public enum CommitBoundary { Prepared, TemporaryFlushed, Replaced, Published }
    public interface ITransactionStore
    {
        TransactionState Read();
        void Write(TransactionState state);
    }
    internal interface ICommandTransactionStore : ITransactionStore
    {
        void Write(TransactionState state, TransactionCommand command);
    }
    public sealed class FileTransactionStore : ITransactionStore
    {
        [Serializable] sealed class Envelope { public string payload, sha256; }
        public string Path { get; }
        public Action<CommitBoundary> Fault { get; set; }
        public FileTransactionStore(string path) { Path = System.IO.Path.GetFullPath(path); }
        public TransactionState Read()
        {
            if (!File.Exists(Path)) return null;
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(Path));
            if (envelope == null || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload) != envelope.sha256)
                throw new InvalidDataException("Transaction checksum không hợp lệ: " + Path);
            return JsonUtility.FromJson<TransactionState>(envelope.payload);
        }
        public void Write(TransactionState state)
        {
            string directory = System.IO.Path.GetDirectoryName(Path);
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None, 1, FileOptions.DeleteOnClose);
            var current = Read();
            if (current != null && current.revision != state.revision - 1 || current == null && state.revision != 0)
                throw new IOException("Durable revision đã thay đổi; cần đọc lại trước khi retry.");
            string temporary = Path + ".tmp";
            string payload = JsonUtility.ToJson(state);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope { payload = payload, sha256 = Hash(payload) }));
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                Fault?.Invoke(CommitBoundary.TemporaryFlushed);
                // Mutation, dedup, receipt và outbox nằm trong cùng một lần thay file; không tạo backup.
                if (File.Exists(Path)) File.Replace(temporary, Path, null);
                else File.Move(temporary, Path);
                Fault?.Invoke(CommitBoundary.Replaced);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static string Hash(string value)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
    }
}
