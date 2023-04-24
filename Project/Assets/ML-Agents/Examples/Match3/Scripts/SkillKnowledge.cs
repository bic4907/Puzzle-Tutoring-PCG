using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MLAgentsExamples
{

    public class SkillKnowledge
    {
        public Dictionary<PieceType, int> CurrentMatchCounts;
        public Dictionary<PieceType, int> TargetMatchCounts;
        
        public PieceType[] PieceTypes = new PieceType[] {PieceType.HorizontalPiece, PieceType.VerticalPiece, PieceType.CrossPiece, PieceType.BombPiece, PieceType.RocketPiece, PieceType.RainbowPiece};

        public int DefaultTargetValue = 5;
        // Start is called before the first frame update
        public SkillKnowledge()
        {
            CurrentMatchCounts = new Dictionary<PieceType, int>();
            TargetMatchCounts = new Dictionary<PieceType, int>();

            for (int i = 0; i < PieceTypes.Length; i++)
            {
                CurrentMatchCounts.Add(PieceTypes[i], 0);
                TargetMatchCounts.Add(PieceTypes[i], DefaultTargetValue);
            }
        }
        
        public SkillKnowledge(int HorizontalPieceCount, 
                            int VerticalPieceCount,
                            int CrossPieceCount,
                            int BombPieceCount,
                            int RocketPieceCount,
                            int RainbowPieceCount) : base()
        {
            TargetMatchCounts[PieceType.HorizontalPiece] = HorizontalPieceCount;
            TargetMatchCounts[PieceType.VerticalPiece] = VerticalPieceCount;
            TargetMatchCounts[PieceType.CrossPiece] = CrossPieceCount;
            TargetMatchCounts[PieceType.BombPiece] = BombPieceCount;
            TargetMatchCounts[PieceType.RocketPiece] = RocketPieceCount;
            TargetMatchCounts[PieceType.RainbowPiece] = RainbowPieceCount;
        }

        public void Reset()
        {
            for (int i = 0; i < PieceTypes.Length; i++)
            {
                CurrentMatchCounts[PieceTypes[i]] = 0;
                TargetMatchCounts[PieceTypes[i]] = DefaultTargetValue;
            }
        }

        public void IncreaseActionMatchCount(PieceType pieceType)
        {
            CurrentMatchCounts[pieceType]++;
        }

        public bool IsActionMatchCountReached(PieceType pieceType)
        {
            return CurrentMatchCounts[pieceType] >= TargetMatchCounts[pieceType];
        }
        
        public Dictionary<PieceType, float> GetActionMatchPercentile()
        {
            Dictionary<PieceType, float> result = new Dictionary<PieceType, float>();
            for (int i = 0; i < PieceTypes.Length; i++)
            {
                result.Add(PieceTypes[i], (float)CurrentMatchCounts[PieceTypes[i]] / (float)TargetMatchCounts[PieceTypes[i]]);
            }

            return result;
        }

    }

    public class SkillKnowledgeExperimentSingle
    {
        public SkillKnowledgeExperimentSingle Instance { 
            get
            { 
                if (Instance == null) 
                {   
                    Instance = new SkillKnowledgeExperimentSingle();
                }
                return Instance;
            } 
            set { value = Instance; }
        }

        private List<SkillKnowledge> SkillKnowledges;

        // Start is called before the first frame update
        public SkillKnowledgeExperimentSingle()
        {
            SkillKnowledges = new List<SkillKnowledge>();
            /*
            player,index,time,event,matched_skill_0,learned_skill_0,matched_skill_1,learned_skill_1,matched_skill_2,learned_skill_2,matched_skill_3,learned_skill_3,matched_skill_4,learned_skill_4,matched_skill_5,learned_skill_5
            5,48,2022-11-08 13:16:42,GameAction,7,1,3,1,0,0,2,1,2,1,3,1
            6,108,2022-11-08 15:20:16,GameAction,11,1,8,1,4,1,3,0,5,1,3,1
            10,97,2022-11-08 19:32:28,GameAction,8,0,4,1,1,1,4,0,3,1,2,1
            11,91,2022-11-11 12:01:32,GameAction,7,1,8,1,2,1,3,1,17,1,4,1
            12,81,2023-01-02 19:09:06,GameAction,6,1,10,1,1,1,2,1,3,1,4,1
            13,98,2023-01-03 15:22:38,GameAction,4,1,3,1,2,1,2,0,6,1,2,0
            14,126,2023-01-03 17:23:28,GameAction,10,1,5,1,1,0,2,0,12,0,4,1
            15,88,2023-01-04 12:46:32,GameAction,9,1,3,1,2,1,1,1,14,1,4,1
            16,76,2023-01-03 19:07:14,GameAction,11,1,7,1,1,1,1,1,8,1,3,1
            17,112,2023-01-04 13:16:17,GameAction,10,1,8,1,7,1,4,1,9,1,5,1
            18,51,2023-01-04 13:28:49,GameAction,4,1,9,1,1,1,2,1,3,1,2,1
            */


            SkillKnowledges.Add(new SkillKnowledge(7, 3, 0, 2, 2, 3));
            SkillKnowledges.Add(new SkillKnowledge(11, 8, 4, 3, 5, 3));
            SkillKnowledges.Add(new SkillKnowledge(8, 4, 1, 4, 3, 2));
            SkillKnowledges.Add(new SkillKnowledge(7, 8, 2, 3, 17, 4));
            SkillKnowledges.Add(new SkillKnowledge(6, 10, 1, 2, 3, 4));
            SkillKnowledges.Add(new SkillKnowledge(4, 3, 2, 2, 6, 2));
            SkillKnowledges.Add(new SkillKnowledge(10, 5, 1, 2, 12, 4));
            SkillKnowledges.Add(new SkillKnowledge(9, 3, 2, 1, 14, 4));
            SkillKnowledges.Add(new SkillKnowledge(11, 7, 1, 1, 8, 3));
            SkillKnowledges.Add(new SkillKnowledge(10, 8, 7, 4, 9, 5));
            SkillKnowledges.Add(new SkillKnowledge(4, 9, 1, 2, 3, 2));
        }
    }


}