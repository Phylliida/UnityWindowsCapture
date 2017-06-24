// WrapperForDuplication.h

#pragma once

using namespace System;

extern "C" {
	__declspec(dllexport) int __cdecl HelloWorld();
}

extern int __cdecl HelloWorld() {

	return DesktopDuplication::PleaseGo::GoPlease();
}
