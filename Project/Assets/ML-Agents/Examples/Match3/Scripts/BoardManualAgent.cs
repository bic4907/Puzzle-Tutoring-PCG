using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.MLAgents.Integrations.Match3;


namespace Unity.MLAgentsExamples
{


    public class BoardManualAgent : MonoBehaviour
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

        private string m_uuid = System.Guid.NewGuid().ToString().Substring(0, 8);

        private PCGStepLog m_Logger;
 
        public int PlayerNumber = 0;

        public int MCTS_Simulation = 300;

        private SkillKnowledge m_SkillKnowledge;
        private SkillKnowledge m_ManualSkillKnowledge;

        private const float k_RewardMultiplier = 0.01f;
        
        public int CurrentEpisodeCount = 0;
        public int TotalStepCount = 0;
        public int CurrentStepCount = 0;
        public int TargetEpisodeCount = -1;
        public int SettleCount = 0;
        public int ChangedCount = 0;
        public int NonChangedCount = 0;

        public float KnowledgeAlmostRatio = 0.75f;
        public int KnowledgeReachStep = -1;
        public int KnowledgeAlmostReachStep = -1; // 3/4 percentqage of the target

        public List<int> ComparisonCounts;
        public bool SaveFirebaseLog = false;

        private FirebaseLogger m_FirebaseLogger;
        
        [Header("")]
        public AgentType agentType = AgentType.Agent;
        public MouseInteraction m_mouseInput;
        public int PopUpQuestionnaireTiming = 5;
        int PopUpQuestionnaireCount = 0;

        protected void Awake()
        {
            Board = GetComponent<Match3Board>();
            m_ModelOverrider = GetComponent<ModelOverrider>();
            m_Logger = new PCGStepLog();

            if (SaveFirebaseLog)
            {
                // Add FirebaseLogger component in this game objct
                if (this.gameObject.GetComponent<FirebaseLogger>() == null)
                {
                    m_FirebaseLogger = this.gameObject.AddComponent<FirebaseLogger>();
                }
            }

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
                    case "kp":  // knowledge percentile
                        generatorRewardType = GeneratorReward.KnowledgePercentile;
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
            if(ParameterManagerSingleton.GetInstance().HasParam("knowledgeAlmostRatio"))
            {
                KnowledgeAlmostRatio = (float)Convert.ToDouble(ParameterManagerSingleton.GetInstance().GetParam("knowledgeAlmostRatio"));
            }

            m_SkillKnowledge = SkillKnowledgeExperimentSingleton.Instance.GetSkillKnowledge(PlayerNumber);
            m_ManualSkillKnowledge = new SkillKnowledge();

            ComparisonCounts = new List<int>();
        }

        public void OnEpisodeBegin()
        {
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
            SettleCount = 0;
            ChangedCount = 0;

            if(TargetEpisodeCount != -1 && CurrentEpisodeCount > TargetEpisodeCount)
            {
                # if UnityEditor
                    UnityEditor.EditorApplication.isPlaying = false;
                # else
                    Application.Quit();
                #endif
            }

            ResetKnowledgeReach();
            ComparisonCounts.Clear();
        }

        private void OnPlayerAction()
        {
            if (SaveFirebaseLog)
            {
                FirebaseLog log = new FirebaseLog();
                log.EpisodeCount = CurrentEpisodeCount;
                log.EpisodeStepCount = CurrentStepCount;
                log.TotalStepCount = TotalStepCount;
                log.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                log.InstanceUUID = m_uuid;
                log.SkillKnowledge = m_ManualSkillKnowledge;
                m_FirebaseLogger.Post(log);
            }

        }

        private void FixedUpdate()
        {
            // Make a move every step if we're training, or we're overriding models in CI.

            AnimatedUpdate();

            // We can't use the normal MaxSteps system to decide when to end an episode,
            // since different agents will make moves at different frequencies (depending on the number of
            // chained moves). So track a number of moves per Agent and manually interrupt the episode.
            if (CurrentStepCount >= MaxMoves)
            {
                // TODO OnGameEnd();
                // EpisodeInterrupted();
            }

            MCTS.Instance.SetRewardMode(generatorRewardType);
            MCTS.Instance.SetSimulationLimit(MCTS_Simulation);
            MCTS.Instance.SetKnowledgeAlmostRatio(KnowledgeAlmostRatio);
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
                // AddReward(k_RewardMultiplier * pointsEarned);

                Board.SpawnSpecialCells();

                var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());  
                foreach (var (type, count) in createdPieces)
                {
                    m_SkillKnowledge.IncreaseMatchCount(type, count);
                    m_ManualSkillKnowledge.IncreaseMatchCount(type, count);
                }
                Board.ExecuteSpecialEffect();

                Board.DropCells();

                switch(generatorType)
                {
                    case GeneratorType.Random:
                        Board.FillFromAbove();
                        break;
                    case GeneratorType.MCTS:
                        MCTS.Instance.FillEmpty(Board, m_SkillKnowledge);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                CurrentStepCount += 1;
                TotalStepCount += 1;
            }

            bool isBoardSettled = false;
            while (!HasValidMoves())
            {
                Board.InitSettled();
                isBoardSettled = true;
            }

            if (isBoardSettled)
            {
                SettleCount += 1;
            }
            
            CheckKnowledgeReach();
            
            // Simulate the board with greedy action
            Move move = GreedyMatch3Solver.GetAction(Board);
            Board.MakeMove(move);
            OnPlayerAction();

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
                    // AddReward(k_RewardMultiplier * pointsEarned);

                    Board.SpawnSpecialCells();

                    var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());  
                    foreach (var (type, count) in createdPieces)
                    {
                        m_SkillKnowledge.IncreaseMatchCount(type, count);
                        m_ManualSkillKnowledge.IncreaseMatchCount(type, count);
                    }
                    Board.ExecuteSpecialEffect();

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
                            MCTS.Instance.FillEmpty(Board, m_SkillKnowledge);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    CurrentStepCount += 1;
                    TotalStepCount += 1;

                    nextState = State.FindMatches;
                    break;
                case State.WaitForMove:
                    bool isBoardSettled = false;
                    nextState = State.WaitForMove;
                    Move move = new Move();
                    
                    while (true)
                    {
                        // Shuffle the board until we have a valid move.
                        bool hasMoves = HasValidMoves();
                        if (hasMoves)
                        {
                            break;
                        }
                        Board.InitSettled();
                        isBoardSettled = true;
                    }
                    
                    if (isBoardSettled)
                    {
                        SettleCount += 1;
                    }


                    switch(agentType)
                    {
                        case AgentType.Agent:
                            move = GreedyMatch3Solver.GetAction(Board);
                            Board.MakeMove(move);
                            OnPlayerAction();

                            nextState = State.FindMatches;
                        break;
                        case AgentType.Human:
                            if(PopUpQuestionnaireCount == PopUpQuestionnaireTiming)
                            {
                                gameObject.transform.Find("QuestionBox").gameObject.SetActive(true);
                                PopUpQuestionnaireCount = 0;
                            }
                            if(m_mouseInput.playerHadVaildAction == true)
                            {
                                move = m_mouseInput.GetMove();
                                Board.MakeMove(move);
                                OnPlayerAction();

                                nextState = State.FindMatches;
                                
                                PopUpQuestionnaireCount += 1;
                            }
                        break;
                    }
                    CheckKnowledgeReach();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_CurrentState = nextState;
        }

        public void ResetKnowledgeReach()
        {
            KnowledgeReachStep = -1;
            KnowledgeAlmostReachStep = -1;
        }


        public void CheckKnowledgeReach()
        {
            if (KnowledgeReachStep == -1 && m_SkillKnowledge.IsAllBlockReachTarget())
            {
                KnowledgeReachStep = CurrentStepCount;
            }
            if (KnowledgeAlmostReachStep == -1 && m_SkillKnowledge.IsAllBlockAlmostReachTarget(KnowledgeAlmostRatio))
            {
                KnowledgeAlmostReachStep = CurrentStepCount;
            }
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

            m_Logger.SkillKnowledge = m_SkillKnowledge;
            m_Logger.InstanceUUID = m_uuid;
            m_Logger.SettleCount = SettleCount;
            m_Logger.ChangedCount = ChangedCount;

            // m_Logger.MeanComparisonCount = ComparisonCounts.Count == 0 ? 0 : (float)ComparisonCounts.Average();
            // m_Logger.StdComparisonCount = ComparisonCounts.Count == 0 ? 0 : (float)CalculateStandardDeviation(ComparisonCounts);

            m_Logger.KnowledgeReachStep = KnowledgeReachStep;
            // m_Logger.KnowledgeAlmostReachStep = KnowledgeAlmostReachStep;
    
            FlushLog(GetMatchResultLogPath(), m_Logger);
        }

        private double CalculateStandardDeviation(List<int> numbers) {
            double mean = numbers.Average();
            double sumOfSquaredDifferences = numbers.Select(num => Mathf.Pow(num - (float)mean, 2)).Sum();
            double variance = sumOfSquaredDifferences / (numbers.Count - 1);
            double stdDev = Mathf.Sqrt((float)variance);
            return stdDev;
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
                    string output = "";
                    output += "EpisodeCount,StepCount,Time,InstanceUUID,SettleCount,ChangedCount,MeanComparisonCount,StdComparisonCount,ReachedKnowledgeStep,AlmostReachedKnowledgeStep,";

                    foreach (PieceType pieceType in BoardPCGAgent.PieceLogOrder)
                    {
                        output += $"Matched_{pieceType},";
                    }

                    foreach (PieceType pieceType in BoardPCGAgent.PieceLogOrder)
                    {
                        output += $"Target_{pieceType},";
                    }
                    output = output.Substring(0, output.Length - 1);

                    sw.WriteLine(output);
                }
            }

            // Append a file to write to csv file 
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(log.ToCSVRoW());
            }
        }


    }


}
