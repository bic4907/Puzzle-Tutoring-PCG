using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using System.Net;


namespace Unity.MLAgentsExamples
{

    public class FirebaseLogger
    {
        // Start is called before the first frame update
        private static FirebaseLogger m_Instance;
        private const string FirebaseUrl = "https://tutoringpcg-default-rtdb.firebaseio.com/gameResult/log.json";

        private string m_ExternalIPAddress;

        public static FirebaseLogger Instance { 
            get
            { 
                if (m_Instance == null) 
                {   
                    m_Instance = new FirebaseLogger();
                }
                return m_Instance;
            } 
            set { value = m_Instance; }
        }

        public FirebaseLogger()
        {
            FetchExternalIPAddress();
        }

        public async void Post(FirebaseLog log)
        {
            await SendPostRequest(log);

        }

        private async Task SendPostRequest(FirebaseLog log)
        {
            Dictionary<string, object> jsonBody = log.ToDict();
            jsonBody.Add("IPAddress", m_ExternalIPAddress);

            string jsonString = JsonConvert.SerializeObject(jsonBody);

            using (HttpClient client = new HttpClient())
            {
                // Create the HTTP content with JSON body
                var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
                
                // Send the POST request asynchronously
                HttpResponseMessage response = await client.PostAsync(FirebaseUrl, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    Debug.LogError("Error sending POST request: " + response.StatusCode);
                }
            }
        }

        private void FetchExternalIPAddress()
        {
            using (var webClient = new WebClient())
            {
                // Make a request to the IP address detection service
                m_ExternalIPAddress = webClient.DownloadString("https://api.ip.pe.kr/");

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