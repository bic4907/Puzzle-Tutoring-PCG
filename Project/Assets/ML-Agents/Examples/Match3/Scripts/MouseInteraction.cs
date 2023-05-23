using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
public class MouseInteraction : MonoBehaviour
{
    public Camera mainCamera;
    
    RaycastHit hit;
    RaycastHit hit2;
    Ray ray;
    public Direction direction;
    Move move;
    public Match3Board Board;
    public bool playerHadVaildAction;
    void Start()
    {
        move = new Move();
        Board = GetComponent<Match3Board>();
        playerHadVaildAction = false;
    }
    void Update()
    {
        WaitForMouseInput();
    }
    public Move GetMove()
    {
        if(playerHadVaildAction)
        {
            playerHadVaildAction = false;
            return move;
        }
        else
        {
            return move;
        }
    }
    Direction GetDirection(int row, int col, int row2, int col2)
    {
        if(row == row2)
        {
            if(col > col2 && col - col2 == 1)
            {
                direction = Direction.Left;
            }
            else
            {
                direction = Direction.Right;
            }
        }
        else if(col == col2)
        {
            if(row > row2 && row - row2 == 1)
            {
                direction = Direction.Down;
            }
            else
            {
                direction = Direction.Up;
            }
        }

        return direction;
    }
    public void WaitForMouseInput()
    {
        // Vector3 mousePos = Input.mousePosition;
        // mousePos = mainCamera.ScreenToWorldPoint(mousePos);
        // Debug.DrawRay(transform.position, mousePos -  transform.position, Color.blue);
            if (Input.GetMouseButtonDown(0))
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray,out hit))
                {
                    hit.transform.position = new Vector3(hit.transform.position.x, hit.transform.position.y, hit.transform.position.z + 2f);
                }
            }
            if(Input.GetMouseButtonUp(0))
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray,out hit2))
                {
                    if(hit.transform.GetInstanceID() != hit2.transform.GetInstanceID())
                    {
                        //Todo: extract row and col even input is two digit
                        var row = (int)char.GetNumericValue(hit.transform.name[1]);
                        var col = (int)char.GetNumericValue(hit.transform.name[4]);
                        
                        var row2 = (int)char.GetNumericValue(hit2.transform.name[1]);
                        var col2 = (int)char.GetNumericValue(hit2.transform.name[4]);

                        GetDirection(row, col, row2, col2);
                        //Todo: Add None move on move
                        move = Move.FromPositionAndDirection(row, col, direction, Board.GetCurrentBoardSize());
                        if(Board.IsMoveValid(move))
                        {
                            playerHadVaildAction = true;    
                        }

                    }
                    else
                    {
                        
                    }
                }
            }
    }
}
