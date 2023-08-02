using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AssetLoader : MonoBehaviour 
{
    byte[] results;
    bool IsDone = false;

    public byte[] LoadAssetBundle(string path)
    {
        // Start the coroutine using the static method
        StartCoroutine(GetAssetBundle(path));

        while(!IsDone) {
        }
        return results;
    }

    private IEnumerator GetAssetBundle(string path)
    {
        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
            IsDone = true;
        }
        else {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
 
            // Or retrieve results as binary data
            results = www.downloadHandler.data;
            IsDone = true;
        }
    }
}