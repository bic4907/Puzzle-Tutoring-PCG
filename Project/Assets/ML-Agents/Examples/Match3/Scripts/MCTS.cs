using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
using System.Linq;

using UnityEngine;


public class Node {
    public int visits;
    public int depth;
    public float score;
    public List<Node> children;
    public Node parent;
    public Match3Board boardState;
    
    public Node(int depth, int visits, float score, List<Node> children, Node parent, Match3Board boardState)
    {
        this.depth = depth + 1;
        this.visits = visits;
        this.score = score;
        this.children = children;
        this.parent = parent;
        this.boardState = boardState;
    }
}

public class MCTS : MonoBehaviour
{
    private Match3Board simulator;
    private Node rootNode;
    private Node currentNode;
    public int numberOfChild;
    public int depthLimit = 2;
    public int simulationStepLimit = 100;
    // Start is called before the first frame update
    void Start()
    {
        simulator = GetComponent<Match3Board>();
        numberOfChild = 1;
        PrepareRootNode();
    }

    // Update is called once per frame
    void Update()
    {
        Search();
    }
    public void PrepareRootNode()
    {
        ResetRootNode();
        currentNode = rootNode;
    }

    public void Search() {
        currentNode = rootNode;
        // Select
        while (currentNode.children.Count > 0) {
            currentNode = SelectBestChild(currentNode);
        }

        // Expand
        if (currentNode.visits > 0) {
            if (depthLimit <= currentNode.depth)
            {
                Expand(currentNode);
            }
            currentNode = SelectBestChild(currentNode);
        }

        // Rollout
         float score = Simulate(currentNode);

        // Backpropagate
        while (currentNode != null) {
            currentNode.visits++;
            currentNode.score += score;
            currentNode = currentNode.parent;
        }
    }
    private void ResetRootNode()
    {
        rootNode = new Node(0, 0, 0f, new List<Node>(), null, simulator);
        Expand(rootNode);
    }
    private Node SelectBestChild(Node node) {
        float bestScore = float.MinValue;
        Node bestChild = null;

        foreach (Node child in node.children) {
            float score = child.score / child.visits + Mathf.Sqrt(2 * Mathf.Log(node.visits) / child.visits);
            if (float.IsNaN(score))
            {
                score = 0;
            }
            if (score > bestScore) {
                bestScore = score;
                bestChild = child;
            }
        }

        return bestChild;
    }

    private void Expand(Node node) {
        for(int i = 0; i<numberOfChild; i++)
        {
            var move = node.boardState.ValidMoves().ToArray()[i];

            var tmpBoard = node.boardState.DeepCopy(this.gameObject);
            tmpBoard.MakeMove(move);

            var tmpChild = new Node(node.depth, 0, 0f, new List<Node>(), node, tmpBoard);
            node.children.Add(tmpChild);
        }
    }

    private float Simulate(Node node) {
        int cnt = 0;
        while(HasValidMoves(node.boardState.ValidMoves()) && simulationStepLimit > cnt)
        {
            var hasMatched = node.boardState.MarkMatchedCells();
            if (hasMatched)
            {
                var pointsEarned = node.boardState.ClearMatchedCells();
                node.boardState.DropCells();
                // Here PCG Part
                // RequestDecision();
                node.boardState.FillFromAbove();

            }
            
            var move = node.boardState.ValidMoves().ToArray()[0];
            node.boardState.MakeMove(move);
            cnt += 1;
        }

        return 1;
    }
    bool HasValidMoves(IEnumerable<Move> board)
    {
        foreach (var unused in board)
        {
            return true;
        }

        return false;
    }
}
