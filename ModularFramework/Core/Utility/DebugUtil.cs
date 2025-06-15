using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModularFramework.Utility
{
    using static EnvironmentConstants;
    public static class DebugUtil {

        public enum LogLevel {
            RUNTIME = 0,
            DEBUG = 1
        }

        private static void Print(LogType type, LogLevel level, string message) {
            bool suppressed = (int)level > DebugLevel;
            if(suppressed) return;
            Debug.unityLogger.Log(type, message);
        }

        private static string CreateLogMessage(string member, string file, int lineNumber, string message)
            => $"[{Path.GetFileName(file)}:{lineNumber} - {member}] {message}";
        
    #region String
        private static void DebugLog(string member, string file, int lineNumber, string message) {
            Print(LogType.Log, LogLevel.DEBUG, CreateLogMessage(member, file, lineNumber, message));
        }

        private static void DebugWarn(string member, string file, int lineNumber, string message) {
            Print(LogType.Warning, LogLevel.DEBUG, CreateLogMessage(member, file, lineNumber, message));
        }

        private static void DebugError(string member, string file, int lineNumber, string message) {
            Print(LogType.Error, LogLevel.DEBUG, CreateLogMessage(member, file, lineNumber, message));
        }

        private static void Log(string member, string file, int lineNumber, string message) {
            Print(LogType.Log, LogLevel.RUNTIME, CreateLogMessage(member, file, lineNumber, message));
        }

        private static void Warn(string member, string file, int lineNumber, string message) {
            Print(LogType.Warning, LogLevel.RUNTIME, CreateLogMessage(member, file, lineNumber, message));
        }

        private static void Error(string member, string file, int lineNumber, string message) {
            Print(LogType.Error, LogLevel.RUNTIME, CreateLogMessage(member, file, lineNumber, message));
        }
    #endregion
    #region Object
        public static void DebugLog<T>(T obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugLog(member, file, line, obj.ToString());
        public static void DebugWarn<T>(T obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugWarn(member, file, line, obj.ToString());
        public static void DebugError<T>(T obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugError(member, file, line, obj.ToString());
        public static void Log<T>(T obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Log(member, file, line, obj.ToString());
        public static void Warn<T>(T obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Warn(member, file, line, obj.ToString());
        public static void Error<T>(T obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Error(member, file, line, obj.ToString());
    #endregion
    #region Enumerable
        public static void DebugLog<T>(IEnumerable<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugLog(member, file, line, obj.Join());
        public static void DebugWarn<T>(IEnumerable<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugWarn(member, file, line, obj.Join());
        public static void DebugError<T>(IEnumerable<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugError(member, file, line, obj.Join());
        public static void Log<T>(IEnumerable<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Log(member, file, line, obj.Join());
        public static void Warn<T>(IEnumerable<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Warn(member, file, line, obj.Join());
        public static void Error<T>(IEnumerable<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Error(member, file, line, obj.Join());

        public static void DebugLog<T>(List<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugLog(member, file, line, obj.Join());
        public static void DebugWarn<T>(List<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugWarn(member, file, line, obj.Join());
        public static void DebugError<T>(List<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugError(member, file, line, obj.Join());
        public static void Log<T>(List<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Log(member, file, line, obj.Join());
        public static void Warn<T>(List<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Warn(member, file, line, obj.Join());
        public static void Error<T>(List<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0)
            => Error(member, file, line, obj.Join());

        public static void DebugLog<T>(HashSet<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugLog(member, file, line, obj.Join());
        public static void DebugWarn<T>(HashSet<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugWarn(member, file, line, obj.Join());
        public static void DebugError<T>(HashSet<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugError(member, file, line, obj.Join());
        public static void Log<T>(HashSet<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0)
            => Log(member, file, line, obj.Join());
        public static void Warn<T>(HashSet<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Warn(member, file, line, obj.Join());
        public static void Error<T>(HashSet<T> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Error(member, file, line, obj.Join());

        public static void DebugLog<TX,TY>(Dictionary<TX,TY> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugLog(member, file, line, obj.Join());
        public static void DebugWarn<TX,TY>(Dictionary<TX,TY> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugWarn(member, file, line, obj.Join());
        public static void DebugError<TX,TY>(Dictionary<TX,TY> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => DebugError(member, file, line, obj.Join());
        public static void Log<TX,TY>(Dictionary<TX,TY> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Log(member, file, line, obj.Join());
        public static void Warn<TX,TY>(Dictionary<TX,TY> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Warn(member, file, line, obj.Join());
        public static void Error<TX,TY>(Dictionary<TX,TY> obj, [CallerMemberName] string member = "", [CallerFilePath] string file="", [CallerLineNumber] int line = 0) 
            => Error(member, file, line, obj.Join());
    #endregion
    }
}