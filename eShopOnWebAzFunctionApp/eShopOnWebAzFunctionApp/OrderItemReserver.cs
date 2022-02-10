using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Azure.Messaging.EventGrid;
using Azure;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;

namespace eShopOnWebAzFunctionApp
{
    public static class OrderItemReserver
    {
        private const string ServiceBusQueueName = "orderqueue";
        private const string ContainerName = "eshopcontainer";
        [FunctionName("OrderItemReserver")]
        [FixedDelayRetry(5, "00:00:10")]
        public static void Run(
            [ServiceBusTrigger(ServiceBusQueueName, Connection = "ServiceBusConnectionString")] string myQueueItem,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=eshopstoragescnt;AccountKey=S8xquWNAG4soUOBTZXrUBozk0sVpHPjgUap0nU+77QN/L7suNyrGEGW62zR7kfQ2dmdOS/AT6nDG+ASt+w/4kA==;EndpointSuffix=core.windows.net");
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = cloudBlobClient.GetContainerReference(ContainerName);
                blobContainer.CreateIfNotExistsAsync();
                string randomStr = Guid.NewGuid().ToString();
                BlobRequestOptions blobRequestOptions = new BlobRequestOptions() { RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.LinearRetry(TimeSpan.FromSeconds(20), 3) };
                CloudBlockBlob blob = blobContainer.GetBlockBlobReference(randomStr);
                List<dynamic> data = JsonConvert.DeserializeObject<List<dynamic>>(myQueueItem);
                List<object> list = new List<object>();
                foreach (var dt in data)
                {
                    list.Add(new
                    {
                        item = dt.ProductName,
                        quantity = dt.Quantity
                    });
                }

                var serializeJesonObject = JsonConvert.SerializeObject(list);
                blob.Properties.ContentType = "application/json";

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(serializeJesonObject)))
                {
                    blob.UploadFromStreamAsync(ms, accessCondition: null, blobRequestOptions, operationContext: null);
                }
                log.LogInformation($"Blob {randomStr} is uploaded to container {blobContainer.Name}");
                blob.SetPropertiesAsync();
                log.LogInformation("Order Items Reserver is executed successfully.");
            }
            catch (Exception ex)
            {
                HttpClient http = new HttpClient();
                var obj = new
                {
                    subject = "Order Item Reserver Failed",
                    data = myQueueItem
                };
                http.PostAsync("https://prod-07.northcentralus.logic.azure.com:443/workflows/4867a215847342138d28c5903607c68d/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=9LjvqRIYhIEODjra2cNUEZyG9IrHQxjl7RCcNyBB4OM",
                    new StringContent(
            JsonConvert.SerializeObject(obj),
            Encoding.UTF8,
            Application.Json));
            }
        }
    }
}
