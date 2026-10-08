using System;
using System.Collections;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class GameSession
    {
        Coroutine checkpointNotice;

        void ShowCheckpointNotice()
        {
            if (!Application.isPlaying)
            {
                Transactions?.DrainCheckpoint();
                Say("Đã lưu trò chơi");
                return;
            }
            if (Transactions?.CheckpointPending != true)
            {
                Say("Đã lưu trò chơi");
                return;
            }
            Say("Đang lưu trò chơi");
            if (checkpointNotice == null) checkpointNotice = StartCoroutine(WaitForCheckpoint());
        }

        IEnumerator WaitForCheckpoint()
        {
            // Chờ qua frame; không block luồng chơi để hiển thị trạng thái lưu.
            yield return null;
            while (true)
            {
                bool pending;
                try
                {
                    pending = Transactions?.CheckpointPending == true;
                }
                catch (Exception error)
                {
                    BlockRecovery(error);
                    break;
                }
                if (!pending)
                {
                    Say("Đã lưu trò chơi");
                    break;
                }
                yield return null;
            }
            checkpointNotice = null;
        }
    }
}
