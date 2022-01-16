using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;

namespace ScalingFunctionTest5601
{
    

    public static class Function1
    {
        static HttpClient client = new HttpClient();

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


#region
            string name = req.Query["name"];

            string uri = Environment.GetEnvironmentVariable("uri");
            //string staticUri = Environment.GetEnvironmentVariable("StaticUri");

            var semaphoreLoopCount = Convert.ToInt32(Environment.GetEnvironmentVariable("semaphoreLoopCount"));
            var totalRequestCount = Convert.ToInt32(Environment.GetEnvironmentVariable("totalRequestCount"));
            #endregion

            //========================

            var semaphore = new SemaphoreSlim(3);

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                await semaphore.WaitAsync();

                HttpRequestMessage msg = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://www.bing.com") /*"https://TrainingScalingFA5601App.azurewebsites.net/api/Function1")*/
                };

                tasks.Add(client.SendAsync(msg).ContinueWith((t) => semaphore.Release()));
            }
            await Task.WhenAll(tasks);

            //========================

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully. {uri.ToString() + " | " + semaphoreLoopCount.ToString() + " | " + totalRequestCount.ToString()}";

            return new OkObjectResult(responseMessage);
        }
    }
}
