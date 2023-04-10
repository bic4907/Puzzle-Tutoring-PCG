using System;
using System.Collections.Generic;
using Unity.MLAgents.Integrations.Match3;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

namespace Unity.MLAgentsExamples
{


    public class Match3Board : AbstractBoard
    {
        public int MinRows;
        [FormerlySerializedAs("Rows")]
        public int MaxRows;

        public int MinColumns;
        [FormerlySerializedAs("Columns")]
        public int MaxColumns;

        public int NumCellTypes;
        // public int NumSpecialTypes;

        public const int k_EmptyCell = -1;
        //[Tooltip("Points earned for clearing a basic cell (cube)")]
        //public int BasicCellPoints = 1;

        //[Tooltip("Points earned for clearing a special cell (sphere)")]
        //public int SpecialCell1Points = 2;
        
        //[Tooltip("Points earned for clearing an extra special cell (plus)")]
        //public int SpecialCell2Points = 3;



        /// <summary>
        /// Seed to initialize the <see cref="System.Random"/> object.
        /// </summary>
        public int RandomSeed;

        (int CellType, int SpecialType)[,] m_Cells;

        // PCG에서 새로 생성된 블럭들을 보관하기 위한 공간
        (int CellType, int SpecialType)[,] m_GeneratedCells;
        bool[,] m_Matched;

        private BoardSize m_CurrentBoardSize;

        System.Random m_Random;

        void Awake()
        {
            m_Cells = new (int, int)[MaxColumns, MaxRows];
            m_Matched = new bool[MaxColumns, MaxRows];

            // Start using the max rows and columns, but we'll update the current size at the start of each episode.
            m_CurrentBoardSize = new BoardSize
            {
                Rows = MaxRows,
                Columns = MaxColumns,
                NumCellTypes = NumCellTypes,
            };
        }

        void Start()
        {
            m_Random = new System.Random(RandomSeed == -1 ? gameObject.GetInstanceID() : RandomSeed);
            InitRandom();
        }

        public override BoardSize GetMaxBoardSize()
        {
            return new BoardSize
            {
                Rows = MaxRows,
                Columns = MaxColumns,
                NumCellTypes = NumCellTypes,
            };
        }

        public override BoardSize GetCurrentBoardSize()
        {
            return m_CurrentBoardSize;
        }

        /// <summary>
        /// Change the board size to a random size between the min and max rows and columns. This is
        /// cached so that the size is consistent until it is updated.
        /// This is just for an example; you can change your board size however you want.
        /// </summary>
        public void UpdateCurrentBoardSize()
        {
            var newRows = m_Random.Next(MinRows, MaxRows + 1);
            var newCols = m_Random.Next(MinColumns, MaxColumns + 1);
            m_CurrentBoardSize.Rows = newRows;
            m_CurrentBoardSize.Columns = newCols;
        }

        public override bool MakeMove(Move move)
        {
            if (!IsMoveValid(move))
            {
                return false;
            }
            var originalValue = m_Cells[move.Column, move.Row];
            var (otherRow, otherCol) = move.OtherCell();
            var destinationValue = m_Cells[otherCol, otherRow];

            m_Cells[move.Column, move.Row] = destinationValue;
            m_Cells[otherCol, otherRow] = originalValue;
            return true;
        }

        public override int GetCellType(int row, int col)
        {
            if (row >= m_CurrentBoardSize.Rows || col >= m_CurrentBoardSize.Columns)
            {
                throw new IndexOutOfRangeException();
            }
            return m_Cells[col, row].CellType;
        }

        public override int GetSpecialType(int row, int col)
        {
            if (row >= m_CurrentBoardSize.Rows || col >= m_CurrentBoardSize.Columns)
            {
                throw new IndexOutOfRangeException();
            }
            return m_Cells[col, row].SpecialType;
        }

        public override bool IsMoveValid(Move m)
        {
            if (m_Cells == null)
            {
                return false;
            }

            return SimpleIsMoveValid(m);
        }


        private bool IsCellInBounds(int col, int row)
        {
            return (row >= 0 && row < m_CurrentBoardSize.Rows) && (col >= 0 && col < m_CurrentBoardSize.Columns);
        }

        public bool MarkMatchedCells(int[,] cells = null)
        {
            ClearMarked();


            bool madeMatch = false;
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    int cellType = m_Cells[j, i].CellType;
                    int specialType = m_Cells[j, i].SpecialType;

                    foreach(KeyValuePair<PieceType, List<int[,]>> matchCase in SpecialMatch.GetInstance().MatchCases)
                    {
                        PieceType pieceType = matchCase.Key;
                        List<int[,]> matchShapes = matchCase.Value;

                        List<int[]> matchedPositions = new List<int[]>(); // matched positions

                        foreach(int[,] shape in matchShapes)
                        {

                            PieceType matchedType = pieceType;
                            for (var k = 0; k < shape.GetLength(0); k++)
                            {
                                for (var l = 0; l < shape.GetLength(1); l++)
                                {
                                    // Check for the exception
                                    if(!IsCellInBounds(j + l, i + k)) {
                                        matchedType = PieceType.None;
                                        break;
                                    }


                                    matchedPositions.Add(new int[] {j + l, i + k});

                                    // Exception for different cell type
                                    if(m_Cells[j + l, i + k].CellType != cellType || m_Cells[j + l, i + k].SpecialType != (int)PieceType.NormalPiece)
                                    {
                                        matchedType = PieceType.None; 
                                        break;
                                    }

                                }

                                if(matchedType == PieceType.None) break;
                            }

                            if(matchedType != PieceType.None) {
                                Debug.Log("Matched " + matchedType + " at " + j + ", " + i);
                                // TODO 생성된 블럭 Created에 넣기

                                foreach(int[] position in matchedPositions)
                                {
                                    m_Matched[position[0], position[1]] = true;
                                    madeMatch = true;
                                }
                            }
                           

                        }
                    }


                }
            }

            return madeMatch;
        }

        /// <summary>
        /// Sets cells that are matched to the empty cell, and returns the score earned.
        /// </summary>
        /// <returns></returns>
        public int ClearMatchedCells()
        {
            var pointsByType = new[] {
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece],
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece],
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece],
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece],
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece],
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece],
                SpecialMatch.GetInstance().CreateScores[PieceType.NormalPiece]
            };

            int pointsEarned = 0;
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    if (m_Matched[j, i])
                    {
                        var speciaType = GetSpecialType(i, j);
                        pointsEarned += pointsByType[speciaType];
                        m_Cells[j, i] = (k_EmptyCell, 0);
                    }
                }
            }

            ClearMarked(); // TODO clear here or at start of matching?
            return pointsEarned;
        }

        public bool DropCells()
        {
            var madeChanges = false;
            // Gravity is applied in the negative row direction
            for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
            {
                var writeIndex = 0;
                for (var readIndex = 0; readIndex < m_CurrentBoardSize.Rows; readIndex++)
                {
                    m_Cells[j, writeIndex] = m_Cells[j, readIndex];
                    if (m_Cells[j, readIndex].CellType != k_EmptyCell)
                    {
                        writeIndex++;
                    }
                }

                // Fill in empties at the end
                for (; writeIndex < m_CurrentBoardSize.Rows; writeIndex++)
                {
                    madeChanges = true;
                    m_Cells[j, writeIndex] = (k_EmptyCell, 0);
                }
            }

            return madeChanges;
        }

        public bool FillFromAbove()
        {
            bool madeChanges = false;
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    if (m_Cells[j, i].CellType == k_EmptyCell)
                    {
                        madeChanges = true;
                        m_Cells[j, i] = (GetRandomCellType(), (int)PieceType.NormalPiece);
                    }
                }
            }

            return madeChanges;
        }

        public (int, int)[,] Cells
        {
            get { return m_Cells; }
        }

        public bool[,] Matched
        {
            get { return m_Matched; }
        }

        // Initialize the board to random values.
        public void InitRandom()
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    m_Cells[j, i] = (GetRandomCellType(), (int)PieceType.NormalPiece);
                }
            }
        }

        public void InitSettled()
        {
            InitRandom();
            while (true)
            {
                var anyMatched = MarkMatchedCells();
                if (!anyMatched)
                {
                    return;
                }
                ClearMatchedCells();

                // Create the spcial blocks to the board (before dropping)



                DropCells();
                FillFromAbove();
            }
        }

        void ClearMarked()
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    m_Matched[j, i] = false;
                }
            }
        }

        int GetRandomCellType()
        {
            return m_Random.Next(0, NumCellTypes);
        }

        int GetRandomSpecialType()
        {
            return m_Random.Next((int)PieceType.NormalPiece, (int)PieceType.RainbowPiece);
        }


    }



}
