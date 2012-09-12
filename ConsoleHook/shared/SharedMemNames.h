#pragma once

//////////////////////////////////////////////////////////////////////////////

class SharedMemNames
{
	public:

		static wformat formatConsoleParams;
		static wformat formatInfo;
		static wformat formatCursorInfo;
		static wformat formatBufferInfo;
		static wformat formatBuffer;
		static wformat formatCopyInfo;
		static wformat formatPasteInfo;
		static wformat formatMouseEvent;
		static wformat formatNewConsoleSize;
		static wformat formatNewScrollPos;

};

//////////////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////////////////

wformat SharedMemNames::formatConsoleParams(L"Console_consoleParams_%1%");
wformat SharedMemNames::formatInfo(L"Console_consoleInfo_%1%");
wformat SharedMemNames::formatCursorInfo(L"Console_cursorInfo_%1%");
wformat SharedMemNames::formatBufferInfo(L"Console_consoleBufferInfo_%1%");
wformat SharedMemNames::formatBuffer(L"Console_consoleBuffer_%1%");
wformat SharedMemNames::formatCopyInfo(L"Console_consoleCopyInfo_%1%");
wformat SharedMemNames::formatPasteInfo(L"Console_consolePasteInfo_%1%");
wformat SharedMemNames::formatMouseEvent(L"Console_formatMouseEvent_%1%");
wformat SharedMemNames::formatNewConsoleSize(L"Console_newConsoleSize_%1%");
wformat SharedMemNames::formatNewScrollPos(L"Console_newScrollPos_%1%");

//////////////////////////////////////////////////////////////////////////////
