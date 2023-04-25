using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
using System.Linq;

using UnityEngine;

namespace Unity.MLAgentsExamples
{
    public enum SimulationType
    {
        Generator = 0,
        Solver = 1,
    }


    public class Node
    {
        public int visits;
        public int depth;
        public float score;
        public List<Node> children;
        public Node parent;
        public Match3Board board;

        public SimulationType simulationType;
        
        public Node(int depth, 
                    int visits,
                    float score,
                    List<Node> children,
                    Node parent,
                    Match3Board board,
                    SimulationType simulationType)
        {
            this.depth = depth + 1;
            this.visits = visits;
            this.score = score;
            this.children = children;
            this.parent = parent;
            this.board = board;
            this.simulationType = simulationType;
        }
    }

    public class MCTS
    {
        private static MCTS _Instance = null;

        public static MCTS Instance { get {
            if (_Instance == null)
            {
                _Instance = new MCTS();
            }
            return _Instance;
        }}

        private Match3Board simulator;
        private Node rootNode;
        private Node currentNode;
        public int numberOfChild;
        public int depthLimit = 2;
        public int simulationStepLimit = 100;

        GameObject m_DummyBoard;

        public MCTS()
        {
            m_DummyBoard = GameObject.Find("DummyBoard").gameObject;
        }

        public void PrepareRootNode()
        {
            ResetRootNode();
            currentNode = rootNode;
        }

        private void ResetRootNode()
        {  
            // TODO Check
            rootNode = new Node(0, 0, 0f, new List<Node>(), null, simulator, SimulationType.Generator);
            Expand(rootNode);
        }

        public void Search()
        {
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

        public void FillEmpty(Match3Board board)
        {
            var _board = board.DeepCopy(m_DummyBoard);

            // Fill Empty cells
            simulator = _board;
            ResetRootNode();
            Search();
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

            for(int i = 0; i < numberOfChild; i++)
            {
                var move = node.board.ValidMoves().ToArray()[i];

                var tmpBoard = node.board.DeepCopy(m_DummyBoard);
                tmpBoard.MakeMove(move);

                var tmpChild = new Node(node.depth, 0, 0f, new List<Node>(), node, tmpBoard, SimulationType.Solver);
                node.children.Add(tmpChild);

            }
        }

        private float Simulate(Node node) {
            
            int score = 0;

            switch (node.simulationType)
            {
                case SimulationType.Generator:

                    // TODO Place a random block in the board
                    

                    break; 
                case SimulationType.Solver:
                    Move move = GreedyMatch3Solver.GetAction(node.board);
                    node.board.MakeMove(move);

                    while (true)
                    {
                        var hasMatched = node.board.MarkMatchedCells();
                        if (!hasMatched)
                        {
                            break;
                        }
                        var pointsEarned = node.board.ClearMatchedCells();
                        node.board.ExecuteSpecialEffect();
                        node.board.SpawnSpecialCells();
                        node.board.DropCells();
                    }

                    while (!HasValidMoves())
                    {
                        // Shuffle the board until we have a valid move.

                        // Backpropagate null
                        Board.InitSettled();
                    }

                    // TODO calculate the score
                    score =  1;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }



            /*
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
            */

            return score;
        }

        /*        
        bool HasValidMoves(IEnumerable<Move> board)
        {
            foreach (var unused in board)
            {
                return true;
            }

            return false;
        }
        */
    }
}