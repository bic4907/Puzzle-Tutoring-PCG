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

            
            SimulationType simType = node.board.HasEmptyCell() ? SimulationType.Generator : SimulationType.Solver;
            Node tmpChild = null;
            Match3Board tmpBoard = null;

            switch(simType)
            {
                case SimulationType.Generator:
                    
                    // Make the children with spawning a colored block in the empty space
                    // TODO random sequence
                    for (int i = 0; i < node.board.NumCellTypes; i++)
                    {
                        tmpBoard = node.board.DeepCopy(m_DummyBoard);
                        tmpBoard.SpawnColoredBlock(i);

                        tmpChild = new Node(node.depth, 0, 0f, new List<Node>(), node, tmpBoard, SimulationType.Generator);
                        node.children.Add(tmpChild);
                    }

                    break;
                case SimulationType.Solver:

                    tmpBoard = node.board.DeepCopy(m_DummyBoard);
                    // Append a child node with heuristic decision
                    Move move = GreedyMatch3Solver.GetAction(tmpBoard);
                    tmpBoard.MakeMove(move);

                    tmpChild = new Node(node.depth, 0, 0f, new List<Node>(), node, tmpBoard, SimulationType.Solver);
                    node.children.Add(tmpChild);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private float Simulate(Node node) {
            
            float score = 0f;
            bool hasMatched;
            switch (node.simulationType)
            {
                case SimulationType.Generator:
                    hasMatched = node.board.MarkMatchedCells();
                    if (hasMatched)
                    {
                        score = -1.0f;
                    }
                    break; 
                case SimulationType.Solver:

                    // TODO Player learning score

                    hasMatched = node.board.MarkMatchedCells();
                    node.board.SpawnSpecialCells();

                    var createdPieces = node.board.GetLastCreatedPiece();
                    foreach (var piece in createdPieces)
                    {
                        PieceType type = (PieceType)piece.SpecialType;              
                        Debug.Log("Special Piece: " + type);
                    }

                    score = 1.0f;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }

            return score;
        }

        private bool IsTerminal(Node node)
        {
            // TODO Add node depth for player simulating
            return HasValidMoves(node.board.ValidMoves());
        }


        private SimulationType GetSimulationType(Match3Board baord) 
        {
            // Return the simulation type whether the empty space is exist in the board
            if (baord.HasEmptyCell())
            {
                return SimulationType.Generator;
            }
            else
            {
                return SimulationType.Solver;
            }
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
}