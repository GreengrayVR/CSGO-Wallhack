using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CSGO_Wallhack
{
    public static class Memory
    {
        public static Process m_Process;
        public static IntPtr m_pProcessHandle;

        public static int clientBase { get; private set; }
        public static int clientSize { get; private set; }

        public static int m_iNumberOfBytesRead = 0;
        public static int m_iNumberOfBytesWritten = 0;

        public static bool Init()
        {
            if (GetHandle("csgo"))
            {
                clientBase = GetModuleAdress("client");
                clientSize = GetModuleSize("client");

                return true;
            }
            else return false;
        }

        public static bool GetHandle(string ProcessName)
        {
            // Check if csgo.exe is running
            if (Process.GetProcessesByName(ProcessName).Length > 0)
                m_Process = Process.GetProcessesByName(ProcessName)[0];
            else
            {
                return false;
            }
            m_pProcessHandle = OpenProcess(0x0008 | 0x0010 | 0x0020 | 0x00100000, false, m_Process.Id); // Sets Our ProcessHandle
            return true;
        }

        public static int GetModuleAdress(string ModuleName)
        {
            try
            {
                foreach (ProcessModule ProcMod in m_Process.Modules)
                {
                    if (!ModuleName.Contains(".dll"))
                        ModuleName = ModuleName.Insert(ModuleName.Length, ".dll");

                    if (ModuleName == ProcMod.ModuleName)
                    {
                        return (int)ProcMod.BaseAddress;
                    }
                }
            }
            catch
            {

            }
            return -1;
        }

        public static int GetModuleSize(string ModuleName)
        {
            try
            {
                foreach (ProcessModule ProcMod in m_Process.Modules)
                {
                    if (!ModuleName.Contains(".dll"))
                        ModuleName = ModuleName.Insert(ModuleName.Length, ".dll");

                    if (ModuleName == ProcMod.ModuleName)
                    {
                        return (int)ProcMod.ModuleMemorySize;
                    }
                }
            }
            catch
            {

            }

            return -1;
        }

        public static T Read<T>(int Adress) where T : struct
        {
            int ByteSize = Marshal.SizeOf(typeof(T)); // Get ByteSize Of DataType
            byte[] buffer = new byte[ByteSize]; // Create A Buffer With Size Of ByteSize
            ReadProcessMemory((int)m_pProcessHandle, Adress, buffer, buffer.Length, ref m_iNumberOfBytesRead); // Read Value From Memory

            return ByteArrayToStructure<T>(buffer); // Transform the ByteArray to The Desired DataType
        }


        public static void Write<T>(int Adress, object Value)
        {
            byte[] buffer = StructureToByteArray(Value); // Transform Data To ByteArray 

            WriteProcessMemory((int)m_pProcessHandle, Adress, buffer, buffer.Length, out m_iNumberOfBytesWritten);
        }





        #region Transformation

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }
        #endregion

        #region DllImports

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            int hProcess,
            int lpBaseAddress,
            byte[] buffer,
            int size,
            ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(
            int hProcess,
            int lpBaseAddress,
            byte[] buffer,
            int size,
            out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(
                  IntPtr hProcess,
                  IntPtr lpThreadAttributes,
                  uint dwStackSize,
                  IntPtr lpStartAddress,
                  IntPtr lpParameter,
                  uint dwCreationFlags,
                  out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern Int32 WaitForSingleObject(
            IntPtr hHandle,
            UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
            IntPtr hObject
        );
        #endregion
    }

    class Program
    {
        public static void WallHack()
        {
            const int dwEntityList = 0x4DA3F44;
            const int dwGlowObjectManager = 0x52EC558;
            const int m_iGlowIndex = 0xA438;
            const int m_iTeamNum = 0xF4;

            while (true)
            {
                int glow_manager = Memory.Read<int>(Memory.clientBase + dwGlowObjectManager);

                if (glow_manager == 0)
                    continue;

                for (int i = 0; i < 32; i++)
                {
                    int entity = Memory.Read<int>(Memory.clientBase + dwEntityList + i * 0x10);

                    if (entity != 0)
                    {
                        int entity_team_id = Memory.Read<int>(entity + m_iTeamNum);
                        int entity_glow = Memory.Read<int>(entity + m_iGlowIndex);

                        if(entity_team_id == 2) // Terrorist
                        {
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0x4, (float)1f); // R
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0x8, (float)0f); // G
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0xC, (float)0f); // B
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0x10, (float)1f); // A
                            Memory.Write<int>(glow_manager + entity_glow * 0x38 + 0x24, (int)1); // Enable GLOW
                        } 
                        else if (entity_team_id == 3)
                        {
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0x4, (float)0f); // R
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0x8, (float)0f); // G
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0xC, (float)1f); // B
                            Memory.Write<float>(glow_manager + entity_glow * 0x38 + 0x10, (float)1f); // A
                            Memory.Write<int>(glow_manager + entity_glow * 0x38 + 0x24, (int)1); // Enable GLOW
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            if (Memory.Init())
                WallHack();
            else
                Console.WriteLine("Dude something didnt work lol");
        }
    }
}