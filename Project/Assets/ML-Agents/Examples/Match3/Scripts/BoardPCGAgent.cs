using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Integrations.Match3;


namespace Unity.MLAgentsExamples
{

    public enum GeneratorType
    {
        Random = 0,
        MCTS = 1,
    }

    public class BoardPCGAgent : Agent
    {
        [HideInInspector]
        public Match3Board Board;

        public float MoveTime = 0.0f;
        public int MaxMoves = 500;

        public SkillKnowledge m_SkillKnowledge;
        State m_CurrentState = State.WaitForMove;
        float m_TimeUntilMove;
        private int m_MovesMade;
        private ModelOverrider m_ModelOverrider;

        public GeneratorType generatorType = GeneratorType.MCTS;

        private const float k_RewardMultiplier = 0.01f;
        protected override void Awake()
        {
            base.Awake();
            Board = GetComponent<Match3Board>();
            m_ModelOverrider = GetComponent<ModelOverrider>();
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            Board.UpdateCurrentBoardSize();
            Board.InitSettled();
            m_CurrentState = State.FindMatches;
            m_TimeUntilMove = MoveTime;
            m_MovesMade = 0;
        }

        private void FixedUpdate()
        {
            // Make a move every step if we're training, or we're overriding models in CI.
            var useFast = Academy.Instance.IsCommunicatorOn || (m_ModelOverrider != null && m_ModelOverrider.HasOverrides);
            if (useFast)
            {
                FastUpdate();
            }
            else
            {
                AnimatedUpdate();
            }

            // We can't use the normal MaxSteps system to decide when to end an episode,
            // since different agents will make moves at different frequencies (depending on the number of
            // chained moves). So track a number of moves per Agent and manually interrupt the episode.
            if (m_MovesMade >= MaxMoves)
            {
                EpisodeInterrupted();
            }
        }

        void FastUpdate()
        {
            while (true)
            {
                var hasMatched = Board.MarkMatchedCells();
                if (!hasMatched)
                {
                    break;
                }
                var pointsEarned = Board.ClearMatchedCells();
                AddReward(k_RewardMultiplier * pointsEarned);
                Board.ExecuteSpecialEffect();
                Board.SpawnSpecialCells();
                Board.DropCells();

                switch(generatorType)
                {
                    case GeneratorType.Random:
                        Board.FillFromAbove();
                        break;
                    case GeneratorType.MCTS:
                        MCTS.Instance.FillEmpty(Board);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                // TODO Fill here with MCTS or random
                // Board.FillFromAbove();
                // MCTS.Search(Board);

            }

            while (!HasValidMoves())
            {
                // Shuffle the board until we have a valid move.
                Board.InitSettled();
            }

            
            // Simulate the board with greedy action
            Move move = GreedyMatch3Solver.GetAction(Board);
            Board.MakeMove(move);

            m_MovesMade++;
        }

        void AnimatedUpdate()
        {
            m_TimeUntilMove -= Time.deltaTime;
            if (m_TimeUntilMove > 0.0f)
            {
                return;
            }

            m_TimeUntilMove = MoveTime;

            State nextState;

            switch (m_CurrentState)
            {
                case State.FindMatches:
                    var hasMatched = Board.MarkMatchedCells();
                    nextState = hasMatched ? State.ClearMatched : State.WaitForMove;
                    if (nextState == State.WaitForMove)
                    {
                        m_MovesMade++;
                    }
                    break;
                case State.ClearMatched:
                    var pointsEarned = Board.ClearMatchedCells();
                    AddReward(k_RewardMultiplier * pointsEarned);
                    

                    Board.ExecuteSpecialEffect();
                    Board.SpawnSpecialCells();
                    nextState = State.Drop;
                    break;
                case State.Drop:
                    Board.DropCells();
                    nextState = State.FillEmpty;
                    break;
                case State.FillEmpty:

                    switch(generatorType)
                    {
                        case GeneratorType.Random:
                            Board.FillFromAbove();
                            break;
                        case GeneratorType.MCTS:
                            MCTS.Instance.FillEmpty(Board);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    nextState = State.FindMatches;
                    break;
                case State.WaitForMove:
                    while (true)
                    {
                        // Shuffle the board until we have a valid move.
                        bool hasMoves = HasValidMoves();
                        if (hasMoves)
                        {
                            break;
                        }
                        Board.InitSettled();
                    }
                    
                    // Simulate the board with greedy action
                    Move move = GreedyMatch3Solver.GetAction(Board);
                    Board.MakeMove(move);
                    
                    nextState = State.FindMatches;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_CurrentState = nextState;
        }

        bool HasValidMoves()
        {
            foreach (var unused in Board.ValidMoves())
            {
                return true;
            }

            return false;
        }

    }

}
