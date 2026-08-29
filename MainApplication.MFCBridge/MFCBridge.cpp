// MFCDialogs.cpp : Defines the initialization routines for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "MFCBridge.h"
#include "resource.h"

#include "afxwin.h"
#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
//TODO: If this DLL is dynamically linked against the MFC DLLs,
//		any functions exported from this DLL which call into
//		MFC must have the AFX_MANAGE_STATE macro added at the
//		very beginning of the function.
//
//		For example:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// normal function body here
//		}
//
//		It is very important that this macro appear in each
//		function, prior to any calls into MFC.  This means that
//		it must appear as the first statement within the
//		function, even before any object variable declarations
//		as their constructors may generate calls into the MFC
//		DLL.
//
//		Please see MFC Technical Notes 33 and 58 for additional
//		details.
//

// CMFCDialogsApp

Graph graph;

class CMFCDialogsApp : public CWinApp
{
private:
public:
	CMFCDialogsApp();

	// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};

BEGIN_MESSAGE_MAP(CMFCDialogsApp, CWinApp)
END_MESSAGE_MAP()


// CMFCDialogsApp construction

CMFCDialogsApp::CMFCDialogsApp()
{
	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
	std::vector<Node> nodes = {
		{"Node1", NodeType::Normal},
		{"Node2", NodeType::Normal},
		{"Node3", NodeType::Constant},
		{"Node4", NodeType::Normal},
		{"Node5", NodeType::Normal},
		{"Node6", NodeType::Constant},
		{"Node7", NodeType::Normal},
		{"Node8", NodeType::WellParameter},
		{"Node9", NodeType::WellParameter},
		{"Node10", NodeType::Decoded},
		{"Node11", NodeType::Decoded},
		{"Node12", NodeType::Normal},
		{"Node13", NodeType::Normal},
		{"Node14", NodeType::Decoded},
		{"Node15", NodeType::Normal}
	};

	graph = Graph(nodes);
	for (int i = 0; i < 30; ++i)
	{
		int r1 = rand() % nodes.size();
		int r2 = rand() % nodes.size();
		if (r1 != r2)
			graph.AddEdge(r1, r2);
	}
	int x = 0;
}

// The one and only CMFCDialogsApp object
CMFCDialogsApp theApp;


// CMFCDialogsApp initialization

BOOL CMFCDialogsApp::InitInstance()
{
	CWinApp::InitInstance();

	return TRUE;
}

class CEmbeddedDlg : public CDialog
{
public:
	CEmbeddedDlg(CWnd* pParent = nullptr) : CDialog(IDD_DIALOG1, pParent) {}
};

CDialog* dlg;

MFC_BRIDGE_API HWND CreateMfcDialog(HWND hParentHwnd)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	auto* pParent = CWnd::FromHandle(hParentHwnd);
	if (dlg != nullptr)
	{
		dlg->DestroyWindow();
		delete dlg;
	}
	dlg = new CDialog(IDD_DIALOG1, pParent);

	dlg->Create(IDD_DIALOG1, pParent);
	dlg->MoveWindow(CRect(0, 0, 500, 500));
	dlg->ShowWindow(SW_SHOW);
	return NULL;
}
MFC_BRIDGE_API Graph GetGraph()
{
	return graph;
}