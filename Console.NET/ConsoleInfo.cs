using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleDotNet
{
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

    [StructLayout(LayoutKind.Sequential)]
    public struct ConsoleBufferInfo
    {
        public int BufferStartIndex;
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
