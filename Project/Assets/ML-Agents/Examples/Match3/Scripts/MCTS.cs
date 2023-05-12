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
        public int playerActionCount;
        public float score;
        public List<Node> children;
        public Node parent;
        public Match3Board board;

        public SimulationType simulationType;
        
        public Node(int depth, 
                    int visits,
                    int playerActionCount,
                    float score,
                    List<Node> children,
                    Node parent,
                    Match3Board board,
                    SimulationType simulationType)
        {
            this.depth = depth;
            this.visits = visits;
            this.playerActionCount = playerActionCount;
            this.score = score;
            this.children = children;
            this.parent = parent;
            this.board = board;
            this.simulationType = simulationType;
        }
        ~Node()
        {
            board = null;
            parent = null;
            children = null;
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


        private float BestBoardScore;
        private Match3Board BestBoard;
        public int numberOfChild;
        public int depthLimit = 2;
        public int simulationStepLimit = 300;

        private int TargetDepth = 0;

        private bool IsChanged = false;
        private int m_ComparisonCount = 0;

        private GeneratorReward RewardMode = GeneratorReward.Score;

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
            rootNode = new Node(0, 0, 0, 0f, new List<Node>(), null, simulator, SimulationType.Generator);
            Expand(rootNode);
        }

        public void Search()
        {
            currentNode = rootNode;

            // Select (Find the terminal node ,tree policy)
            while (currentNode.children.Count > 0)
            {
                currentNode = SelectBestChild(currentNode);
            }

            // Expand
            if (currentNode.visits == 0) {
                Expand(currentNode);
                currentNode = SelectBestChild(currentNode);
            }

            // Rollout (Default policy)
            float score = Simulate(currentNode);

            // Backpropagate
            while (currentNode != null) {
                currentNode.visits++;
                currentNode.score += score;
                
                if (currentNode.depth == TargetDepth)
                {
                    if (currentNode.score > BestBoardScore || BestBoard == null)
                    {
                        BestBoardScore = currentNode.score;
                        BestBoard = currentNode.board;

                        IsChanged = true;
                        m_ComparisonCount += 1;


                        // On the best node was searched

                    }
                }

                currentNode = currentNode.parent;
            }
        }

        public bool FillEmpty(Match3Board board)
        {
            
            // Print the empty cell count

            var _board = board.DeepCopy();

            // Initialize the searching process
            simulator = _board;
            m_ComparisonCount = 0;

            // Fill Empty cells
            PrepareRootNode();
            PrepareSearch();

            for(int i = 0; i < simulationStepLimit; i++)
            {
                Search();
            }

            board.m_Cells = ((int CellType, int SpecialType)[,])BestBoard.m_Cells.Clone();

            this.rootNode = null;

            // Return if the board is changed

            // Debug.Log($"IsChanged: {IsChanged}, BestBoardScore: {BestBoardScore}");
            return IsChanged;
        }


        private void PrepareSearch()
        {
            TargetDepth = simulator.GetEmptyCellCount();
            BestBoardScore = -1;

            BestBoard = simulator.DeepCopy();
            BestBoard.FillFromAbove();

            IsChanged = false;
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

        private int[] GetRandomIntArray(int maxVal)
        {
            int[] randomArray = new int[maxVal];
            System.Random random = new System.Random();

            for (int i = 0; i < randomArray.Length; i++) {
                randomArray[i] = i;
            }

            for (int i = randomArray.Length - 1; i > 0; i--) {
                int j = random.Next(i + 1);
                int temp = randomArray[i];
                randomArray[i] = randomArray[j];
                randomArray[j] = temp;
            }

            return randomArray;
        }

        private void Expand(Node node) {

            // print the node depth and isEmpty
            // Debug.Log($"Node Depth: {node.depth}, IsEmpty: {node.board.HasEmptyCell()}");

            SimulationType simType = node.board.HasEmptyCell() ? SimulationType.Generator : SimulationType.Solver;
            Node tmpChild = null;
            Match3Board tmpBoard = null;


            switch(simType)
            {
                case SimulationType.Generator:
                    
                    // Make the children with spawning a colored block in the empty space
                    // TODO random sequence

                    int[] randomArray = GetRandomIntArray(node.board.NumCellTypes);

                    for (int i = 0; i < randomArray.Length; i++)
                    {
                        tmpBoard = node.board.DeepCopy();
                        tmpBoard.SpawnColoredBlock(randomArray[i]);

                        tmpChild = new Node(node.depth + 1, 0, node.playerActionCount, 0f, new List<Node>(), node, tmpBoard, SimulationType.Generator);
                        node.children.Add(tmpChild);
                    }

                    break;
                case SimulationType.Solver:
                    Debug.Log("Solver");
                    tmpBoard = node.board.DeepCopy();
                    // Append a child node with heuristic decision
                    Move move = GreedyMatch3Solver.GetAction(tmpBoard);
                    tmpBoard.MakeMove(move);

                    tmpChild = new Node(node.depth + 1, 0, node.playerActionCount + 1, 0f, new List<Node>(), node, tmpBoard, SimulationType.Solver);
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
                    // hasMatched = node.board.MarkMatchedCells();
                    // if (hasMatched)
                    // {
                    //     score = -1.0f;
                    // }
                    break; 
                case SimulationType.Solver:
                    // Make move


                    hasMatched = node.board.MarkMatchedCells();
                    node.board.ClearMatchedCells();
                    // node.board.ExecuteSpecialEffect();
                    node.board.SpawnSpecialCells();
                    // node.board.DropCells();

                    var createdPieces = node.board.GetLastCreatedPiece();
                    foreach (var piece in createdPieces)
                    {
                        PieceType type = (PieceType)piece.SpecialType;
                        Debug.Log($"Created Piece: {type}");
                    }

                    switch(RewardMode)
                    {
                        case GeneratorReward.Score:
                            score += createdPieces.Count;

                            Debug.Log("Score: " + score);

                            break;
                        case GeneratorReward.Knowledge:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }

            return score;
        }

        private bool IsTerminal(Node node)
        {
            // TODO Add node depth for player simulating
            return HasValidMoves(node.board.ValidMoves()) || node.playerActionCount >= 1;
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

        public void SetRewardMode(GeneratorReward mode)
        {
            this.RewardMode = mode;
        }
        
        
        public void SetSimulationLimit(int limit)
        {
            this.simulationStepLimit = limit;
        }

        public int GetComparisonCount()
        {
            return m_ComparisonCount;
        }

    }
}