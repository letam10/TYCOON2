using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Tycoon
{
    // Mỗi command được flush trước publish. Checkpoint gom journal vào save v2, giữ đủ dedup.
    public sealed class GameplayTransactionStore : ICommandTransactionStore, IDisposable
    {
        [Serializable] sealed class Entry { public long revision;public double time;public TransactionCommand command; }
        [Serializable] sealed class Envelope {public string payload,sha256;}
        readonly GameSession game;
        readonly FileStream lease;
        readonly CheckpointWriter checkpoint;
        FileStream journalWriter;
        bool disposed;
        long durableRevision=-1;
        double checkpointTime;
        string Journal=>game.SavePath+".journal";
        public Action<CommitBoundary> Fault {get;set;}
        internal static FileStream AcquireLease(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None, 1, FileOptions.DeleteOnClose);
        }
        public GameplayTransactionStore(GameSession game, FileStream existingLease = null)
        {
            this.game=game;
            checkpoint = new CheckpointWriter(game.SavePath);
            lease = existingLease ?? AcquireLease(game.SavePath);
            try { TrimPartialTail(Journal); }
            catch { lease.Dispose(); throw; }
            Application.quitting += DrainOnQuit;
        }
        public TransactionState Read()
        {
            checkpoint.Observe(true);
            var state = SaveStore.Read(game.SavePath)?.transactionState;
            durableRevision = state?.revision ?? -1;
            checkpointTime = state?.simulationTime ?? 0;
            return state;
        }
        public void Write(TransactionState state)
        {
            checkpoint.Observe(true);
            if(durableRevision!=-1&&durableRevision!=state.revision-1)throw new IOException("Save revision đã thay đổi.");
            SaveStore.Write(game.SavePath,RuntimeTransactions.Project(state,game.CaptureSaveData()),Fault);
            durableRevision=state.revision;
        }
        void ICommandTransactionStore.Write(TransactionState state,TransactionCommand command)
        {
            checkpoint.Observe(false);
            if(state.revision!=durableRevision+1)throw new IOException("Journal revision đã thay đổi; cần recovery.");
            // Lệnh chỉ đổi ví không tick clock; thời gian journal vẫn phải sau snapshot đã chụp.
            double time = Math.Max(checkpointTime, state.simulationTime);
            string payload = JsonUtility.ToJson(new Entry { revision = state.revision, time = time, command = command });
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope{payload=payload,sha256=FileTransactionStore.Hash(payload)})+"\n");
            Fault?.Invoke(CommitBoundary.TemporaryFlushed);
            var stream = JournalWriter();
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
            durableRevision=state.revision;Fault?.Invoke(CommitBoundary.Replaced);
        }
        public void Checkpoint(TransactionState state)
        {
            if (!CanCheckpoint) return;
            CheckpointOwned(CheckpointSnapshot.Copy(state));
        }
        internal bool CanCheckpoint => Fault != null || checkpoint.Ready;
        internal void DrainCheckpoint() => checkpoint.Observe(true);
        internal void CheckpointOwned(TransactionState state)
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameplayTransactionStore));
            if(state.revision!=durableRevision)throw new IOException("Checkpoint revision không khớp journal.");
            var world = CheckpointSnapshot.Copy(game.CaptureSaveData());
            var data = RuntimeTransactions.ProjectOwned(state, world);
            checkpointTime = Math.Max(checkpointTime, state.simulationTime);
            if (Fault == null)
            {
                checkpoint.Start(data);
                return;
            }
            checkpoint.Observe(true);
            SaveStore.WriteCheckpoint(game.SavePath, data, Fault);
            ClearJournal();
        }
        void ClearJournal()
        {
            // Crash sau replace vẫn an toàn: recovery bỏ qua command đã nằm trong checkpoint.
            var stream = JournalWriter();
            stream.SetLength(0);
            stream.Position = 0;
            stream.Flush(true);
        }
        void DrainOnQuit()
        {
            try { checkpoint.Observe(true); }
            catch (Exception error) { game.BlockRecovery(error); }
        }
        FileStream JournalWriter()
        {
            if (journalWriter != null) return journalWriter;
            // Giữ handle trong phiên để tránh mở/đóng file ở mỗi giao dịch; vẫn flush trước publish.
            journalWriter = new FileStream(Journal, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            journalWriter.Position = journalWriter.Length;
            return journalWriter;
        }
        internal static SaveData Recover(string path,SaveData data)
        {
            string journal=path+".journal";
            if(data.transactionState==null||!File.Exists(journal))return data;
            using var reader = new StreamReader(new FileStream(journal, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite));
            string text = reader.ReadToEnd();
            int complete = text.LastIndexOf('\n');
            if(complete<0)return data;
            TransactionCore core=null;double now=data.transactionState.simulationTime;
            long initialRevision=data.transactionState.revision;
            foreach(string line in text.Substring(0,complete).Split('\n'))
            {
                if(string.IsNullOrWhiteSpace(line))continue;
                var envelope=JsonUtility.FromJson<Envelope>(line);
                if(envelope==null||string.IsNullOrEmpty(envelope.payload)||FileTransactionStore.Hash(envelope.payload)!=envelope.sha256)
                    throw new InvalidDataException("Journal checksum không hợp lệ.");
                var entry=JsonUtility.FromJson<Entry>(envelope.payload);
                if(entry==null||entry.command==null||!double.IsFinite(entry.time))throw new InvalidDataException("Journal thiếu command.");
                if(entry.revision<=initialRevision)continue;
                core??=new TransactionCore(data.transactionState,null,()=>now,true,replaying:true);
                if(entry.revision!=core.Revision+1||entry.time<now)throw new InvalidDataException("Journal mất thứ tự hoặc thời gian.");
                now=entry.time;core.Execute(entry.command);
            }
            return core==null?data:RuntimeTransactions.Project(core.Snapshot(),data);
        }
        static void TrimPartialTail(string path)
        {
            if(!File.Exists(path))return;
            using var stream=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.Read);
            long length=stream.Length;if(length==0)return;
            stream.Position=length-1;if(stream.ReadByte()=='\n')return;
            long position=length-1;
            while(position>=0){stream.Position=position;if(stream.ReadByte()=='\n')break;position--;}
            stream.SetLength(position+1);stream.Flush(true);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Application.quitting -= DrainOnQuit;
            try
            {
                checkpoint.Observe(true);
                if (checkpoint.HasWritten)
                {
                    // Sau drain mới replay phần đuôi rồi compact; command mới không bị xóa giữa chừng.
                    var data = SaveStore.Read(game.SavePath);
                    SaveStore.WriteCheckpoint(game.SavePath, data);
                    ClearJournal();
                }
            }
            finally
            {
                try { journalWriter?.Dispose(); }
                finally { lease.Dispose(); }
            }
        }
    }
}
