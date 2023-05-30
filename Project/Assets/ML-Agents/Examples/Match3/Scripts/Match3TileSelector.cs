using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match3TileSelector : MonoBehaviour
{
    public GameObject emptyTile;
    public GameObject[] tileTypes = new GameObject[0];
    public Material[] materialTypes = new Material[0];
    public GameObject explosionPrefab;
    private Dictionary<int, MeshRenderer> tileDict = new Dictionary<int, MeshRenderer>();
    bool corutineControlFlag = true;

    // Start is called before the first frame update
    void Awake()
    {
        for (int i = 0; i < tileTypes.Length; i++)
        {
            tileDict.Add(i, tileTypes[i].GetComponent<MeshRenderer>());
        }

        SetActiveTile(0, 0);
    }

    public void AllTilesOff()
    {
        foreach (var item in tileTypes)
        {
            item.SetActive(false);
        }
    }

    public void SetActiveTile(int typeIndex, int matIndex, bool isHumanControlled = false)
    {
        if (matIndex == -1)
        {
            AllTilesOff();
            emptyTile.SetActive(true);
            corutineControlFlag = true;
        }
        else
        {
            emptyTile.SetActive(false);
            for (int i = 0; i < tileTypes.Length; i++)
            {
                if (i == typeIndex)
                {
                    tileTypes[i].SetActive(true);
                    tileDict[i].sharedMaterial = materialTypes[matIndex];
                    if(corutineControlFlag && isHumanControlled)
                    {
                        StartCoroutine(ScaleTile(tileDict[i].transform.localScale, i));
                        corutineControlFlag = false;
                    }
                }
                else
                {
                    tileTypes[i].SetActive(false);
                }
            }
        }
    }
    public void ExplodeTile()
    {
        var tmp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(tmp, 1f);
    }
    //corutine to scale the tile
    public IEnumerator ScaleTile(Vector3 scale, int i)
    {
        float time = 0;
        tileTypes[i].transform.localScale = transform.localScale * 0.1f;
        while (time < 3)
        {
            time += Time.deltaTime;
            tileTypes[i].transform.localScale = Vector3.Lerp(tileTypes[i].transform.localScale, scale, time / 6);
            yield return null;
        }
    }
}
