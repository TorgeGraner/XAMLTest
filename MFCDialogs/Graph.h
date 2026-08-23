#pragma once
#include <string>
#include <vector>
#include <queue>
#include <unordered_map>

enum NodeType
{
	Constant,
	WellParameter,
	Decoded,
	Normal
};

struct Node
{
	std::string name;
	NodeType type;
};

class Graph
{
private:
	std::vector<Node> _nodes;
	std::vector<std::vector<int>> _adjacencyMatrix;
public:
	Graph() = default;
	Graph(const std::vector<Node>& nodes) : _nodes(nodes) 
	{
		_adjacencyMatrix.resize(_nodes.size()); 
	}

	int AddNode(const Node& node)
	{
		int id = std::ssize(_nodes);
		_nodes.push_back(node);
		_adjacencyMatrix.resize(id + 1);
		return id;
	}
	void AddEdge(int fromId, int toId)
	{
		if (_adjacencyMatrix.size() > fromId)
		{
			const auto neighbors = _adjacencyMatrix[fromId];
			if (std::find(neighbors.begin(), neighbors.end(), toId) == neighbors.end())
			{
				_adjacencyMatrix[fromId].push_back(toId);
			}
		}
	}

	int GetNodeIdByName(const std::string& name) const
	{
		for (int i = 0; i < _nodes.size(); ++i)
		{
			if (_nodes[i].name == name)
			{
				return i;
			}
		}
		return -1; // Not found
	}

	Graph GetReachableSubgraph(const std::string& nodeName) const
	{
		const int startId = GetNodeIdByName(nodeName);
		if (startId == -1)
		{
			return Graph(); // Return empty graph if node not found
		}
		Graph subgraph;
		std::queue<int> queue;
		queue.push(startId);
		std::vector<int> visited;
		std::unordered_map<int, int> idToSubgraphId;

		idToSubgraphId[startId] = subgraph.AddNode(_nodes[startId]);

		while(!queue.empty())
		{
			int currentNodeId = queue.front();
			queue.pop();
			visited.push_back(currentNodeId);
			for (int neighborId : _adjacencyMatrix[currentNodeId])
			{
				if (std::find(visited.begin(), visited.end(), neighborId) == visited.end())
				{

					if (idToSubgraphId.find(neighborId) == idToSubgraphId.end())
					{
						idToSubgraphId[neighborId] = subgraph.AddNode(_nodes[neighborId]);
					}
					subgraph.AddEdge(idToSubgraphId[currentNodeId], idToSubgraphId[neighborId]);
				}
			}
		}
		return subgraph;
	}

	std::vector<int> GetNeighbors(int nodeId) const
	{
		if (nodeId < 0 || nodeId >= _adjacencyMatrix.size())
		{
			return {};
		}
		return _adjacencyMatrix[nodeId];
	}
	std::vector<Node> GetNodes() const
	{
		return _nodes;
	}
};

