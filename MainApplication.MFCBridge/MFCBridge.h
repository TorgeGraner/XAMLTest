#pragma once
#include "Graph.h"
#include "Windows.h"

#ifndef MFCDIALOGS_EXPORTS
#define MFC_BRIDGE_API __declspec(dllexport)
#else
#define MFC_BRIDGE_API __declspec(dllimport)
#endif

MFC_BRIDGE_API HWND CreateMfcDialog(HWND hParentHwnd);
MFC_BRIDGE_API Graph GetGraph();