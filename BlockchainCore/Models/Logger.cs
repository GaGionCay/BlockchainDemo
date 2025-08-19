using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainCore.Models
{
    public static class Logger
    { 
        public static bool EnableInfoLogs = true; 
        public static bool EnableDebugLogs = false;

        public static void LogInfo(string message)
        {
            if (EnableInfoLogs)
            {
                Console.WriteLine($"[INFO] {message}");
            }
        }

        public static void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }

        public static void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                Console.WriteLine($"[ERROR] {message} - {ex.Message}");
            }
            else
            {
                Console.WriteLine($"[ERROR] {message}");
            }
        }

        public static void LogDebug(string message)
        {
            if (EnableDebugLogs)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }
    }
}
