#pragma once

#ifndef MFCDIALOGS_EXPORTS
#define DLL_EXPORT __declspec(dllexport)
#else
#define DLL_EXPORT __declspec(dllimport)
#endif

extern "C" 
{
	DLL_EXPORT int32_t GetNodeCount();
	DLL_EXPORT int32_t GetNeighborCount(int32_t nodeId);
	DLL_EXPORT int32_t GetNeighborId(int32_t nodeId, int32_t neighborIndex);
	DLL_EXPORT void GetNodeName(int32_t nodeId, char* buffer, size_t bufferSize);
	DLL_EXPORT HWND CreateMfcDialog(HWND hParentHwnd);
	DLL_EXPORT void ResizeMfcDialog(HWND hDlg, int width, int height);
}
