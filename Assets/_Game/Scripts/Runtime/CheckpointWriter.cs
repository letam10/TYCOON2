using System;
using System.IO;
using System.Threading.Tasks;

namespace Tycoon
{
    internal sealed class CheckpointWriter
    {
        readonly string path;
        Task pending;
        internal Action BeforeWrite = null;
        internal bool HasWritten { get; private set; }

        internal CheckpointWriter(string path)
        {
            this.path = Path.GetFullPath(path);
        }

        internal bool Ready
        {
            get
            {
                Observe(false);
                return pending == null;
            }
        }

        internal void Start(SaveData ownedSnapshot)
        {
            if (!Ready) throw new InvalidOperationException("Checkpoint đang chạy.");
            var beforeWrite = BeforeWrite;
            pending = Task.Run(() =>
            {
                beforeWrite?.Invoke();
                SaveStore.WriteCheckpoint(path, ownedSnapshot);
            });
        }

        internal void Observe(bool wait)
        {
            if (pending == null || !wait && !pending.IsCompleted) return;
            try
            {
                pending.GetAwaiter().GetResult();
                HasWritten = true;
                pending = null;
            }
            catch (Exception error)
            {
                // Giữ task lỗi để mọi lần drain đều thấy lỗi trước khi ghi tiếp.
                throw new IOException("Không ghi được checkpoint nền.", error);
            }
        }
    }
}
