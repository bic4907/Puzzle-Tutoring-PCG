using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;


namespace Unity.MLAgentsExamples
{

    public class FirebaseLogger
    {
        // Start is called before the first frame update
        private static FirebaseLogger m_Instance;
        private const string FirebaseUrl = "https://tutoringpcg-default-rtdb.firebaseio.com/gameResult/log.json";



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


        public async void SendGameResult()
        {
            Debug.Log("Sending POST request to Firebase...");
            await SendPostRequest();

        }

        private async Task SendPostRequest()
        {
            Dictionary<string, object> jsonBody = new Dictionary<string, object>
            {
                { "name", "John" },
                { "age", 30 }
            };

            string jsonString = JsonConvert.SerializeObject(jsonBody);

            using (HttpClient client = new HttpClient())
            {
                // Create the HTTP content with JSON body
                var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
                Debug.Log(jsonString);
                // Send the POST request asynchronously
                HttpResponseMessage response = await client.PostAsync(FirebaseUrl, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log(response.Content.ReadAsStringAsync().Result);
                    Debug.Log("POST request sent successfully.");
                }
                else
                {
                    Debug.LogError("Error sending POST request: " + response.StatusCode);
                }
            }
        }

    }

}