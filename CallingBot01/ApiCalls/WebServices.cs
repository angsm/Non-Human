using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace CallingBot01//CallingBot01.ApiCalls
{
    public class QAndA
    {
        private string answer;
        private string question;
        private string kbId = "799da174-884d-41e6-848a-03ce175a3fb6";
        private string kbKey = "f01eb562898341b78dbee26391b81e68";

        public QAndA() {

        }

        public string test(string userInput) {

            question = userInput;
            string jsonResponse = string.Empty;
            jsonResponse = askQns(userInput);
            answer = answerQns(jsonResponse);

            return answer;
        }
        
        private string askQns(string message)
        {
            string responseString = string.Empty;

            var query = message; //User Query
            var knowledgebaseId = kbId; // Use knowledge base id created.
            var qnamakerSubscriptionKey = kbKey; //Use subscription key assigned to you.

            //Build the URI
            Uri qnamakerUriBase = new Uri("https://westus.api.cognitive.microsoft.com/qnamaker/v1.0");
            var builder = new UriBuilder($"{qnamakerUriBase}/knowledgebases/{knowledgebaseId}/generateAnswer");
            Debug.WriteLine(builder);

            //Add the question as part of the body
            var postBody = $"{{\"question\": \"{query}\"}}";

            //Send the POST request
            using (WebClient client = new WebClient())
            {
                //Set the encoding to UTF8
                client.Encoding = System.Text.Encoding.UTF8;

                //Add the subscription key header
                client.Headers.Add("Ocp-Apim-Subscription-Key", qnamakerSubscriptionKey);
                client.Headers.Add("Content-Type", "application/json");
                responseString = client.UploadString(builder.Uri, postBody);
                Debug.WriteLine(responseString);
    }
                        
            return responseString;
        }

        private string answerQns(string jsonResponse) {
            string answerString = string.Empty;
            dynamic data = JObject.Parse(jsonResponse);

            //De-serialize the response
            try
            {
                if (data.score > 75)
                {
                    answerString = data.answer;
                }
                else
                {
                    answerString = $"I'm not sure what you meant by '{question}'...";
                }
            }
            catch
            {
                throw new Exception("Unable to deserialize QnA Maker response string.");
            }

            return answerString;
        }
    }
}