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
        long durableRevision=-1;
        string Journal=>game.SavePath+".journal";
        public Action<CommitBoundary> Fault {get;set;}
        public GameplayTransactionStore(GameSession game)
        {
            this.game=game;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(game.SavePath)));
            lease=new FileStream(game.SavePath+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose);
            TrimPartialTail(Journal);
        }
        public TransactionState Read()
        {
            var state=SaveStore.Read(game.SavePath)?.transactionState;durableRevision=state?.revision??-1;return state;
        }
        public void Write(TransactionState state)
        {
            if(durableRevision!=-1&&durableRevision!=state.revision-1)throw new IOException("Save revision đã thay đổi.");
            SaveStore.Write(game.SavePath,RuntimeTransactions.Project(state,game.CaptureSaveData()),Fault);
            durableRevision=state.revision;
        }
        void ICommandTransactionStore.Write(TransactionState state,TransactionCommand command)
        {
            if(state.revision!=durableRevision+1)throw new IOException("Journal revision đã thay đổi; cần recovery.");
            string payload=JsonUtility.ToJson(new Entry{revision=state.revision,time=state.simulationTime,command=command});
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope{payload=payload,sha256=FileTransactionStore.Hash(payload)})+"\n");
            Fault?.Invoke(CommitBoundary.TemporaryFlushed);
            using(var stream=new FileStream(Journal,FileMode.Append,FileAccess.Write,FileShare.Read))
            {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            durableRevision=state.revision;Fault?.Invoke(CommitBoundary.Replaced);
        }
        public void Checkpoint(TransactionState state)
        {
            if(state.revision!=durableRevision)throw new IOException("Checkpoint revision không khớp journal.");
            SaveStore.Write(game.SavePath,RuntimeTransactions.Project(state,game.CaptureSaveData()),Fault);
            // Crash sau replace vẫn an toàn: recovery bỏ qua command đã nằm trong checkpoint.
            using var stream=new FileStream(Journal,FileMode.Create,FileAccess.Write,FileShare.Read);stream.Flush(true);
        }
        internal static SaveData Recover(string path,SaveData data)
        {
            string journal=path+".journal";
            if(data.transactionState==null||!File.Exists(journal))return data;
            string text=File.ReadAllText(journal);int complete=text.LastIndexOf('\n');
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
        public void Dispose()=>lease.Dispose();
    }
}
