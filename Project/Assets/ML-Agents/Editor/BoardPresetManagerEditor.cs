using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.MLAgentsExamples
{
    [CustomEditor(typeof (BoardPresetManager))]
    public class BoardPresetManagerEditor: Editor
    {

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI ();  

            BoardPresetManager bpm = target as BoardPresetManager;
            if (GUILayout.Button("Save Board")) {

                if (bpm)
                {
                    bpm.SaveBoard();
                }

            }
            if (GUILayout.Button("Load Board")) {

                if (bpm)
                {
                    bpm.LoadBoard();
                }

            }
        }

    }
}