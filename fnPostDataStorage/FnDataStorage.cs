using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace fnPostDataStorage
{
    public class FnDataStorage
    {
        private readonly ILogger<FnDataStorage> _logger;

        public FnDataStorage(ILogger<FnDataStorage> logger)
        {
            _logger = logger;
        }

        [Function("dataStorage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processando a Imagem no Storage");

            if (!req.Headers.TryGetValue("file-type", out var fileTypeHeader))
            {
                return new BadRequestObjectResult("Cabeçalho 'file-type' obrigatório.");
            }

            var fileType = fileTypeHeader.ToString();
            var form = await req.ReadFormAsync();
            var file = form.Files["file"];

            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult("Arquivo não enviado.");

            }

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var containerName = fileType;

            BlobClient blobClient = new BlobClient(connectionString, containerName, file.FileName);
            BlobContainerClient blobContainerClient = new BlobContainerClient(connectionString, containerName);

            await blobContainerClient.CreateIfNotExistsAsync();
            await blobContainerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);

            string blobName = file.FileName;
            var blob = blobContainerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, true);
            }

            _logger.LogInformation("Enviado com sucesso.");

            return new OkObjectResult(new
            {
                Message = $"Arquivo {file.FileName} armazenado com sucesso.",
                BlobUri = blob.Uri
            });


        }
    }
}
