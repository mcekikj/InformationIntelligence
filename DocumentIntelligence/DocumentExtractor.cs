using System;
using Azure;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.AI.DocumentIntelligence;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using DocumentIntelligence.Configuration;
using System.Threading.Tasks;

namespace DocumentIntelligence
{
    public class DocumentExtractor
    {
        [FunctionName("DocumentExtractor")]
        public async Task Run([BlobTrigger("intelli-docs/{name}", Connection = "docsintelliconnection")] BlobClient myBlob, string name, ILogger log)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var logLevel = "Information";
            var dateTimeStamp = DateTime.UtcNow;

            var blobUri = myBlob.Uri;

            Console.WriteLine($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blobUri} Bytes");
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blobUri} Bytes");

            string endpoint = DocumentIntelligenceConfiguration.Endpoint;
            string key = DocumentIntelligenceConfiguration.Key;
            AzureKeyCredential credential = new AzureKeyCredential(key);

            // Azure Blob Storage configuration  
            string blobConnectionString = DocumentIntelligenceConfiguration.BlobStorageConnectionString;
            string containerName = DocumentIntelligenceConfiguration.BlobStorageContainerName;
            string blobName = DocumentIntelligenceConfiguration.BlobStorageBlobName;

            DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            Uri fileUri = new Uri(blobUri.ToString());

            try
            {
                AnalyzeDocumentContent content = new AnalyzeDocumentContent()
                {
                    UrlSource = fileUri
                };

                AnalyzeDocumentOperation operation = client.AnalyzeDocumentFromUri(WaitUntil.Completed, "prebuilt-read", fileUri);

                Azure.AI.FormRecognizer.DocumentAnalysis.AnalyzeResult result = operation.Value;

                var logDetails = new StringBuilder();

                foreach (Azure.AI.FormRecognizer.DocumentAnalysis.DocumentLine extractedLineContent in result.Pages[0].Lines)
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    log.LogInformation($"{extractedLineContent.Content.ToString()}");
                    logDetails.AppendLine($"{logLevel}--[{dateTimeStamp}]-{extractedLineContent.Content.ToString()}");
                    Console.WriteLine($"{extractedLineContent.Content.ToString()}");
                }

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@""))
                {
                    file.WriteLine(logDetails.ToString());
                }

                await AppendToBlobStorage(blobConnectionString, containerName, blobName, await AzureOpenAIHelper.LLMSummarization(logDetails.ToString()));
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        static async Task AppendToBlobStorage(string connectionString, string containerName, string blobName, string text)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await containerClient.ExistsAsync())
            {
                await containerClient.CreateAsync();
            }

            var appendText = Encoding.UTF8.GetBytes(text + Environment.NewLine);

            if (await blobClient.ExistsAsync())
            {
                var existingBlob = await blobClient.DownloadContentAsync();
                var existingContent = existingBlob.Value.Content.ToArray();
                var combinedContent = new byte[existingContent.Length + appendText.Length];
                Buffer.BlockCopy(existingContent, 0, combinedContent, 0, existingContent.Length);
                Buffer.BlockCopy(appendText, 0, combinedContent, existingContent.Length, appendText.Length);

                await blobClient.UploadAsync(new BinaryData(combinedContent), true);
            }
            else
            {
                await blobClient.UploadAsync(new BinaryData(appendText), true);
            }
        }
    }
}