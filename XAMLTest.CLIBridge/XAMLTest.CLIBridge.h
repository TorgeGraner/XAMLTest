#pragma once
#include "MFCBridge.h"

using namespace System;
using namespace System::Collections::Generic;

namespace XAMLTest
{
	namespace CLIBridge
	{
		public ref class ManagedNode
		{
		public:
			String^ name;
			int type;
		};
		public ref class ManagedGraph
		{
		public:
			Dictionary<int, String^>^ _labels;
			Dictionary<int, int>^ _types;
			Dictionary<int, List<int>^>^ _adjacencyList;

		};
		public ref class Wrapper
		{
		public:
			static ManagedGraph^ GetManagedGraph();
		};
	}
}
