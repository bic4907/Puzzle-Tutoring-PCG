using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization; 
 

namespace Unity.MLAgentsExamples
{

    public class BoardPresetManager : MonoBehaviour
    {
        private Match3Board board;
        
        void Start()
        {
            if (GetComponent<BoardManualAgent>() != null)
            {
                board = GetComponent<BoardManualAgent>().Board;
            }
            if (GetComponent<BoardPCGAgent>() != null)
            {
                board = GetComponent<BoardPCGAgent>().Board;
            }
            Debug.Log("BoardPresetManager: " + board);
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void SaveBoard()
        {
            Debug.Log("BoardPresetManager: SaveBoard");
            // Get filename with date and time

            string filename = "board_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".bin";
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            board.SaveTo(filePath);
        }

        public void LoadBoard(string filename)
        {
            filename = filename + ".bin";
            Debug.Log($"BoardPresetManager: LoadBoard ({filename})");
            // Get filename with date and time
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            board.LoadFrom(filePath);
        }
    }


    [Serializable]
    public class SerializableBoard
    {
        public (int CellType, int SpecialType)[,] m_Cells;

        public SerializableBoard(Match3Board board)
        {
            m_Cells = ((int CellType, int SpecialType)[,])board.m_Cells.Clone();
        }

    }
}
