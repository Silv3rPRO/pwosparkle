using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PWOProtocol
{
    public class MapLoader
    {
        private class Unrar
        {
            public const int RAR_OM_LIST = 0;
            public const int RAR_OM_EXTRACT = 1;

            [StructLayout(LayoutKind.Sequential)]
            public struct RARHeaderData
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string ArcName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string FileName;
                public uint Flags;
                public uint PackSize;
                public uint UnpSize;
                public uint HostOS;
                public uint FileCRC;
                public uint FileTime;
                public uint UnpVer;
                public uint Method;
                public uint FileAttr;
                public string CmtBuf;
                public uint CmtBufSize;
                public uint CmtSize;
                public uint CmtState;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct RAROpenArchiveData
            {
                public string ArcName;
                public uint OpenMode;
                public uint OpenResult;
                public string CmtBuf;
                public uint CmtBufSize;
                public uint CmtSize;
                public uint CmtState;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct RARHeaderDataEx
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string ArcName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string FileName;
                public string FileNameW;
                public uint Flags;
                public uint PackSize;
                public uint PackSizeHigh;
                public uint UnpSize;
                public uint UnpSizeHigh;
                public uint HostOS;
                public uint FileCRC;
                public uint FileTime;
                public uint UnpVer;
                public uint Method;
                public uint FileAttr;
                public string CmtBuf;
                public uint CmtBufSize;
                public uint CmtSize;
                public uint CmtState;
                public uint Reserved;
            };

            [DllImport("unrar.dll")]
            public static extern IntPtr RAROpenArchive(ref RAROpenArchiveData ArchiveData);
            [DllImport("unrar.dll")]
            public static extern int RARCloseArchive(IntPtr hArcData);
            [DllImport("unrar.dll")]
            public static extern int RARReadHeader(IntPtr hArcData, ref RARHeaderData HeaderData);
            [DllImport("unrar.dll")]
            public static extern int RARProcessFile(IntPtr hArcData, int Operation, string DestPath, string DestName);
            [DllImport("unrar.dll")]
            public static extern void RARSetPassword(IntPtr hArcData, byte[] password);

            public void UnpackArchive(string filename, string destinationFolder, byte[] password)
            {
                RAROpenArchiveData archiveData = new RAROpenArchiveData();
                RARHeaderData headerData = new RARHeaderData();

                archiveData.ArcName = filename + '\0';
                archiveData.OpenMode = RAR_OM_EXTRACT;
                archiveData.CmtBuf = null;

                IntPtr rarHandle = RAROpenArchive(ref archiveData);
                try
                {
                    if (archiveData.OpenResult != 0)
                    {
                        throw new Exception("Could not open the RAR archive " + filename);
                    }
                    RARSetPassword(rarHandle, password);
                    if (RARReadHeader(rarHandle, ref headerData) != 0)
                    {
                        throw new Exception("Could not read any file in the RAR archive " + filename);
                    }
                    if (RARProcessFile(rarHandle, 2, destinationFolder, null) != 0)
                    {
                        throw new Exception("Could not unpack the file in the RAR archive " + filename);
                    }
                }
                finally
                {
                    RARCloseArchive(rarHandle);
                }
            }
        }

        public Map Map { get; private set; }

        private byte[] PASSWORD = { 0x33, 0x65, 0x63, 0x02, 0x67, 0x24, 0x33, 0x03, 0x27, 0x30, 0x27, 0x1B, 0x7E, 0x04, 0x0B, 0x29, 0x39, 0x42, 0x03, 0x60, 0x05, 0x42, 0x34 };

        private WebClient _client;
        private string _content;

        public MapLoader()
        {
            _client = new WebClient { Proxy = null };
        }

        public async Task Load(string mapServer, string mapName)
        {
            await Task.Run(delegate
            {
                if (!Directory.Exists("Maps"))
                {
                    Directory.CreateDirectory("Maps");
                }
            });

            string remoteFile = mapServer + mapName + ".rar";
            string localFile = "Maps/" + mapName + ".rar";
            string localFileUnencrypted = "Maps/" + mapName;

            if (!File.Exists(localFileUnencrypted))
            {
                await _client.DownloadFileTaskAsync(remoteFile, localFile);
                Unrar rar = new Unrar();
                await Task.Run(() => rar.UnpackArchive(localFile, "Maps", PASSWORD));
                await Task.Run(() => File.Delete(localFile));
            }
            await Task.Run(delegate
            {
                _content = File.ReadAllText(localFileUnencrypted);
            });

            Map = new Map(_content);
        }
    }
}
