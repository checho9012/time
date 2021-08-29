using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using time.Common.Models;
using time.Common.Responses;
using time.Functions.Entities;

namespace time.Functions.Function
{
    public static class TimeApi
    {
        [FunctionName(nameof(CreateTime))]
        public static async Task<IActionResult> CreateTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "time")] HttpRequest req,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            ILogger log)
        {
            log.LogInformation("Recieved a new time.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Time time = JsonConvert.DeserializeObject<Time>(requestBody);

            if (string.IsNullOrEmpty(time?.EmployeId.ToString()) ||
                string.IsNullOrEmpty(time?.Date.ToString()) ||
                string.IsNullOrEmpty(time?.Type.ToString()))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a EmployeId, Date and Type"
                });
            }

            TimeEntity timeEntity = new TimeEntity
            {
                PartitionKey = "TIME",
                RowKey = Guid.NewGuid().ToString(),
                ETag = "*",
                EmployeId = time.EmployeId,
                Date = time.Date,
                Type = time.Type,
                IsConsolidated = false
            };

            TableOperation addOperation = TableOperation.Insert(timeEntity);
            await timeTable.ExecuteAsync(addOperation);

            string message = "New time stored in table.";
            log.LogInformation(message);

            return new OkObjectResult(new Response 
            { 
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }
    }
}
