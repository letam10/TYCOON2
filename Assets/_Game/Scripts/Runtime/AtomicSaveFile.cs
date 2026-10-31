using System.IO;
using System.Text;
using System.Threading;

namespace Tycoon
{
    public static class AtomicSaveFile
    {
        public static string ReadText(string path)
        {
            // Reader giữ snapshot inode cũ nhưng không chặn ReplaceFile của checkpoint nền.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        public static void Replace(string temporary, string destination)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    File.Replace(temporary, destination, null);
                    return;
                }
                catch (IOException error) when (attempt < 4 && IsTemporaryConflict(error)
                    && File.Exists(temporary) && File.Exists(destination))
                {
                    // Chỉ retry mã giữ nguyên cả hai tên file; tuyệt đối không xóa save để né lỗi.
                    Thread.Sleep(10 << attempt);
                }
            }
        }

        static bool IsTemporaryConflict(IOException error)
        {
            int code = error.HResult & 0xffff;
            return code is 32 or 33 or 1175;
        }
    }
}
