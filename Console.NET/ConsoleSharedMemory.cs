using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleDotNet
{
    /*
     * Note:
     * C# int type is used in place of C++ DWORD (unsigned long) type to avoid casting uint to int.
     */

    [StructLayout(LayoutKind.Sequential)]
    public struct ConsoleCopyInfo
    {
        public COORD CoordStart;
        public COORD CoordEnd;
        public bool NoWrap;
        public bool TrimSpaces;
        public CopyNewLineChar CopyNewLineChar;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ConsoleParams
    {
        public int ConsoleMainThreadId;
        public int ParentProcessId;
        public int NotificationTimeout;
        public int RefreshInterval;
        public int Rows;
        public int Columns;
        public int BufferRows;
        public int BufferColumns;

        public int MaxRows;
        public int MaxColumns;
        public IntPtr ConsoleWindowHandle;
        public int HookThreadId;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ConsoleBufferInfo
    {
        [FieldOffset(0)]
        public bool NewDataFound;
        [FieldOffset(4)]
        public bool CursorPositionChanged;
        [FieldOffset(8)]
        public int BufferStartIndex;
        [FieldOffset(12)]
        public int BufferSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ConsoleSizeInfo
    {
        public int Rows;
        public int Columns;

        public int ResizeWindowEdge; // window edge used for resizing, one of WMSZ constants
    }

    public enum CopyNewLineChar
    {
        NewLineCrLf = 0,
        NewLineLf = 1
    }
}
