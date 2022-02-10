using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eShopOnWebAzFunctionApp
{
    public static class DeliveryOrderProcess
    {
        private const string _databaseName = "eshopcosmoswebdb";
        private const string _collectionName = "eshopcontainer";
        [FunctionName("DeliveryOrderProcess")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
            databaseName:_databaseName,
            collectionName: _collectionName,
            ConnectionStringSetting ="CosmosDbConnectionString"
            )] IAsyncCollector<dynamic> content,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject<dynamic>(requestBody);

                await content.AddAsync(new
                {
                    id = System.Guid.NewGuid().ToString(),
                    shippingAddress = data.ShippingAddress,
                    items = data.Items,
                    finalPrice = data.FinalPrice
                });

                log.LogInformation($"Order details is uploaded ");
                return new OkObjectResult("Order details is saved successfully.");
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
