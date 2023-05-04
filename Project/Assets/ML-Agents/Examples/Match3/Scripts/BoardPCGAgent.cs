using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Integrations.Match3;
using UnityEditor;


namespace Unity.MLAgentsExamples
{


    public class BoardPCGAgent : Agent
    {
        [HideInInspector]
        public Match3Board Board;

        public float MoveTime = 0.0f;
        public int MaxMoves = 500;

        State m_CurrentState = State.WaitForMove;
        float m_TimeUntilMove;
        private int m_MovesMade;
        private ModelOverrider m_ModelOverrider;

        public bool useForcedFast = true;

        public GeneratorType generatorType = GeneratorType.MCTS;

        public GeneratorReward generatorRewardType = GeneratorReward.Score;

        public static PieceType[] PieceLogOrder = new PieceType[6] {PieceType.HorizontalPiece, PieceType.VerticalPiece, PieceType.CrossPiece, PieceType.RocketPiece, PieceType.BombPiece, PieceType.RainbowPiece};

        private string m_uuid = System.Guid.NewGuid().ToString();

        private PCGStepLog m_Logger;
 
        public int PlayerNumber = -1;

        public int MCTS_Simulation = 300;

        private SkillKnowledge m_SkillKnowledge;

        public int TargetSkill0 { get { return m_SkillKnowledge.CurrentMatchCounts[PieceType.HorizontalPiece]; } }

        private const float k_RewardMultiplier = 0.01f;
        
        public int CurrentEpisodeCount = 0;
        public int CurrentStepCount = 0;

        public int TargetEpisodeCount = -1;

        protected override void Awake()
        {
            base.Awake();
            Board = GetComponent<Match3Board>();
            m_ModelOverrider = GetComponent<ModelOverrider>();
            m_Logger = new PCGStepLog();


            // Parsing the augments
            if(ParameterManagerSingleton.GetInstance().HasParam("targetPlayer"))
            {
                PlayerNumber = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("targetPlayer"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("method"))
            {
                string _method = Convert.ToString(ParameterManagerSingleton.GetInstance().GetParam("method"));

                switch (_method)
                {
                    case "mcts":
                        generatorType = GeneratorType.MCTS;
                        break;
                    case "random":
                        generatorType = GeneratorType.Random;
                        break;
                }
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("objective"))
            {
                string _objective = Convert.ToString(ParameterManagerSingleton.GetInstance().GetParam("objective"));

                switch (_objective)
                {
                    case "score":
                        generatorRewardType = GeneratorReward.Score;
                        break;
                    case "knowledge":
                        generatorRewardType = GeneratorReward.Knowledge;
                        break;
                }
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("mctsSimulation"))
            {
                MCTS_Simulation = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("mctsSimulation"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("targetEpisodeCount"))
            {
                TargetEpisodeCount = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("targetEpisodeCount"));
            }

            m_SkillKnowledge = SkillKnowledgeExperimentSingleton.Instance.GetSkillKnowledge(PlayerNumber);
            
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            Board.UpdateCurrentBoardSize();
            Board.InitSettled();
            m_CurrentState = State.FindMatches;
            m_TimeUntilMove = MoveTime;
            m_MovesMade = 0;

            if (m_Logger != null && CurrentEpisodeCount != 0)
            {
                RecordResult();
            }

            m_Logger = new PCGStepLog();
            m_SkillKnowledge = SkillKnowledgeExperimentSingleton.Instance.GetSkillKnowledge(PlayerNumber);
            
            CurrentEpisodeCount += 1;
            CurrentStepCount = 0;

            if(TargetEpisodeCount != -1 && CurrentEpisodeCount > TargetEpisodeCount)
            {
                # if UnityEditor
                    UnityEditor.EditorApplication.isPlaying = false;
                # else
                    Application.Quit();
                #endif
            }

        }

        private void FixedUpdate()
        {
            // Make a move every step if we're training, or we're overriding models in CI.
            var useFast = Academy.Instance.IsCommunicatorOn || (m_ModelOverrider != null && m_ModelOverrider.HasOverrides);
            if (useFast || useForcedFast)
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
            if (CurrentStepCount >= MaxMoves)
            {
                EpisodeInterrupted();
            }

            MCTS.Instance.SetRewardMode(generatorRewardType);
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

                var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());  
                foreach (var (type, count) in createdPieces)
                {
                    m_SkillKnowledge.IncreaseMatchCount(type, count);
                }
            
                Board.DropCells();

                switch(generatorType)
                {
                    case GeneratorType.Random:
                        Board.FillFromAbove();
                        break;
                    case GeneratorType.MCTS:
                        bool _isChanged = MCTS.Instance.FillEmpty(Board);
                        
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                CurrentStepCount += 1;
            }

            while (!HasValidMoves())
            {
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

                    var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());  
                    foreach (var (type, count) in createdPieces)
                    {
                        m_SkillKnowledge.IncreaseMatchCount(type, count);
                    }
                
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
                    CurrentStepCount += 1;

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

        public void RecordResult()
        {
            // Make a new PCG Log file with the parameters

            m_Logger.EpisodeCount = CurrentEpisodeCount;
            m_Logger.StepCount = CurrentStepCount;
            m_Logger.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            // TODO : Add the instance UUID to the log
            m_Logger.SkillKnowledge = m_SkillKnowledge;
            m_Logger.InstanceUUID = m_uuid;

            FlushLog(GetMatchResultLogPath(), m_Logger);
        }

        private string GetMatchResultLogPath()
        {
            return ParameterManagerSingleton.GetInstance().GetParam("logPath") + 
            "MatchResult_" + ParameterManagerSingleton.GetInstance().GetParam("runId") + "_" + m_uuid + ".csv";
        }

        public void FlushLog(string filePath, PCGStepLog log)
        {
            if (!File.Exists(filePath))
            {
                // Print whether the file exists or not
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.Write("EpisodeCount,StepCount,Time,InstanceUUID,");

                    // Write the CSV header with the content and result index which are named Content[0] with repeated number of times

                    //Also write the result index
                    foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
                    {
                        sw.Write($"Matched_{pieceType},");
                    }

                    foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
                    {
                        sw.Write($"Target_{pieceType},");
                    }

                    sw.WriteLine();
                }
            }

            // Append a file to write to csv file 
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(log.ToCSVRoW());
            }
        }


    }

    public class PCGStepLog
    {
        public int EpisodeCount;
        public int StepCount;
        public string Time;
        public string InstanceUUID;
        public SkillKnowledge SkillKnowledge;

        public PCGStepLog()
        {
           
        }


        public string ToCSVRoW()
        {
            string row = "";
            row += EpisodeCount + ",";
            row += StepCount + ",";
            row += Time + ",";
            row += InstanceUUID + ",";

            foreach (Dictionary<PieceType, int> table in new Dictionary<PieceType, int>[2] { SkillKnowledge.CurrentMatchCounts, SkillKnowledge.TargetMatchCounts })
            {
                foreach (PieceType pieceType in BoardPCGAgent.PieceLogOrder)
                {
                    row += table[pieceType] + ",";
                }
            }
        
            // Remove the last comma
            row = row.Substring(0, row.Length - 1);

            return row;
        }
    }

    public enum GeneratorType
    {
        Random = 0,
        MCTS = 1,
    }


    public enum GeneratorReward
    {
        Score = 0,
        Knowledge = 1,
    }


}
