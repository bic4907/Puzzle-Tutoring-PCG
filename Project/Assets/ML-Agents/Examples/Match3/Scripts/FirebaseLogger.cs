using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;



namespace Unity.MLAgentsExamples
{

    public class FirebaseLogger: MonoBehaviour
    {
        // Start is called before the first frame update
        private const string FirebaseUrl = "https://tutoringpcg-default-rtdb.firebaseio.com/gameResult/log.json";

        private string m_ExternalIPAddress;

        void Start()
        {
            StartCoroutine(FetchExternalIPAddress());

        }

        public void Post(FirebaseLog log)
        {
            StartCoroutine(SendPostRequest(log));

        }


        private IEnumerator SendPostRequest(FirebaseLog log)
        {
            Dictionary<string, object> jsonBody = log.ToDict();
            jsonBody.Add("IPAddress", m_ExternalIPAddress);

            string jsonString = JsonConvert.SerializeObject(jsonBody);

            using (UnityWebRequest request = UnityWebRequest.Post(FirebaseUrl, ""))
            {

                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");
                
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("Error sending POST request: " + request.responseCode);
                }
            }


        }


        private IEnumerator FetchExternalIPAddress()
        {

            using (UnityWebRequest request = UnityWebRequest.Get("https://api.ip.pe.kr/"))
            {
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    m_ExternalIPAddress = request.downloadHandler.text;
                    Debug.Log("External IP Address: " + m_ExternalIPAddress);
                }
                else
                {
                    Debug.LogError("Error fetching external IP address: " + request.error);
                }
            }
        }

    }


    public class FirebaseLog
    {
        public int EpisodeCount;
        public int EpisodeStepCount;
        public int TotalStepCount;
        public string Time;
        public string InstanceUUID;
        public SkillKnowledge SkillKnowledge;

        public FirebaseLog()
        {
           
        }

        public Dictionary<string, object> ToDict()
        {
            // Reigster all local variables
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("EpisodeCount", EpisodeCount);
            dict.Add("EpisodeStepCount", EpisodeStepCount);
            dict.Add("TotalStepCount", TotalStepCount);
            dict.Add("Time", Time);
            dict.Add("InstanceUUID", InstanceUUID);

            dict.Add("CurrentMatches", SkillKnowledge.CurrentMatchCounts);
            dict.Add("CurrentLearned", SkillKnowledge.ManualCheck);

            return dict;
        }
    }


}