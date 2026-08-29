#include "pch.h"

#include "XAMLTest.CLIBridge.h"
#include "msclr\marshal_cppstd.h"
#include "Graph.h"

using namespace msclr::interop;

namespace XAMLTest
{
	namespace CLIBridge
	{
		ManagedGraph^ Wrapper::GetManagedGraph()
		{
			auto labels = gcnew Dictionary<int, String^>();
			auto types = gcnew Dictionary<int, int>();
			auto adjacencyList = gcnew Dictionary<int, List<int>^>();

			Graph graph = GetGraph();
			ManagedGraph^ result = gcnew ManagedGraph();

			auto nodes = graph.GetNodes();

			int id = 0;
			for (const auto& node : nodes)
			{
				labels->Add(id, marshal_as<String^>(node.name));
				types->Add(id, (int)node.type);
				auto neighborList = gcnew List<int>();
				for (const auto& neighbor : graph.GetNeighbors(id))
				{
					neighborList->Add(neighbor);
				}
				adjacencyList->Add(id, neighborList);

				id++;
			}
			result->_labels = labels;
			result->_types= types;
			result->_adjacencyList = adjacencyList;

			return result;
		}
	}
}