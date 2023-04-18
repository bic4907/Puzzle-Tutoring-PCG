using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgentsExamples;


namespace Unity.MLAgentsExamples
{

    public class SpecialMatch
    {

        public static SpecialMatch Instance;
        
        public Dictionary<PieceType, List<int[,]>> MatchCases;
        public Dictionary<PieceType, int> CreateScores;
        public Dictionary<PieceType, int> DestroyScores;

        public static SpecialMatch GetInstance()
        {
            if (Instance == null)
            {
                Instance = new SpecialMatch();
            }
            return Instance;
        }


        public SpecialMatch()
        {
            InitializeMatchCase();
            InitializeCreateScore();
            InitializeDestroyScore();
        }
        
        private void InitializeMatchCase()
        {
            MatchCases = new Dictionary<PieceType, List<int[,]>>();

            MatchCases.Add(PieceType.VerticalPiece, new List<int[,]>());
            MatchCases.Add(PieceType.HorizontalPiece, new List<int[,]>());
            MatchCases.Add(PieceType.NormalPiece, new List<int[,]>());

            MatchCases.Add(PieceType.RocketPiece, new List<int[,]>());
            MatchCases.Add(PieceType.CrossPiece, new List<int[,]>());
            MatchCases.Add(PieceType.BombPiece, new List<int[,]>());
            MatchCases.Add(PieceType.RainbowPiece, new List<int[,]>());


            MatchCases[PieceType.NormalPiece].Add(new int[1, 3] { { 1, 1, 1 } });
            MatchCases[PieceType.NormalPiece].Add(new int[3, 1] { { 1 }, { 1 }, { 1 } });

            MatchCases[PieceType.HorizontalPiece].Add(new int[1, 4] { { 1, 1, 1, 1 } });
            MatchCases[PieceType.VerticalPiece].Add(new int[4, 1] { { 1 }, { 1 }, { 1 }, { 1 } });


            // MatchCases[PieceType.RocketPiece].Add(new int[2, 2] { { 1, 1 }, { 1, 1 } });

            // MatchCases[PieceType.CrossPiece].Add(new int[3, 3] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 } });
            // MatchCases[PieceType.CrossPiece].Add(new int[3, 3] { { 1, 0, 0 }, { 1, 1, 1 }, { 1, 0, 0 } });

            // MatchCases[PieceType.BombPiece].Add(new int[3, 3] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 0, 0 } });
            // MatchCases[PieceType.BombPiece].Add(new int[3, 3] { { 1, 1, 1 }, { 0, 0, 1 }, { 0, 0, 1 } });

            // MatchCases[PieceType.RainbowPiece].Add(new int[1, 5] { { 1, 1, 1, 1, 1 } });
            // MatchCases[PieceType.RainbowPiece].Add(new int[5, 1] { { 1 }, { 1 }, { 1 }, { 1 }, { 1 } });
        }

        private void InitializeCreateScore()
        {
            CreateScores = new Dictionary<PieceType, int>();


            CreateScores.Add(PieceType.Empty, 0);
            CreateScores.Add(PieceType.NormalPiece, 10);
            CreateScores.Add(PieceType.HorizontalPiece, 20);
            CreateScores.Add(PieceType.VerticalPiece, 30);
            CreateScores.Add(PieceType.CrossPiece, 40);
            CreateScores.Add(PieceType.BombPiece, 50);
            CreateScores.Add(PieceType.RocketPiece, 60);
            CreateScores.Add(PieceType.RainbowPiece, 70);
        }

        private void InitializeDestroyScore()
        {
            DestroyScores = new Dictionary<PieceType, int>();
        
            DestroyScores.Add(PieceType.Empty, 0);
            DestroyScores.Add(PieceType.NormalPiece, 100);
            DestroyScores.Add(PieceType.HorizontalPiece, 200);
            DestroyScores.Add(PieceType.VerticalPiece, 300);
            DestroyScores.Add(PieceType.CrossPiece, 400);
            DestroyScores.Add(PieceType.BombPiece, 500);
            DestroyScores.Add(PieceType.RocketPiece, 600);
            DestroyScores.Add(PieceType.RainbowPiece, 700);
        }

        public int GetCreateScore(PieceType pieceType)
        {
            return CreateScores[pieceType];
        }

        public int GetDestroyScore(PieceType pieceType)
        {
            return DestroyScores[pieceType];
        }

    }
}