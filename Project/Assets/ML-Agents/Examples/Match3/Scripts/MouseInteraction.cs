using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgentsExamples;

public class MouseInteraction : MonoBehaviour
{
    public Camera mainCamera;
    
    RaycastHit hit;
    RaycastHit hit2;
    Ray ray;
    Move move;
    bool vaildMove = false;
    bool isDifferenctObj = false;

    public Move WaitForMouseInput()
    {
        // Vector3 mousePos = Input.mousePosition;
        // mousePos = mainCamera.ScreenToWorldPoint(mousePos);
        // Debug.DrawRay(transform.position, mousePos -  transform.position, Color.blue);
        while(!isDifferenctObj || !vaildMove)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("clicked");
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray,out hit))
                {
                    Debug.Log(hit.transform.name);
                    // hit.transform.GetComponent<Renderer>().material.color = 
                    // Color.red;
                }
            }
            if(Input.GetMouseButtonUp(0))
            {
                Debug.Log("Released");
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray,out hit2))
                {
                    Debug.Log(hit2.transform.name);
                    if(hit.transform.GetInstanceID() != hit2.transform.GetInstanceID())
                    {
                        isDifferenctObj = true;
                        vaildMove = true;
                        Debug.Log("Diff");
                    }
                    else
                    {
                        Debug.Log("Same");
                    }
                }
            }
        }
        isDifferenctObj = false;
        vaildMove = false;
        return move;
    }
}
