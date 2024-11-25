using Azure;
using Azure.AI.OpenAI;
using DocumentIntelligence.Configuration;
using System;
using System.Threading.Tasks;

namespace DocumentIntelligence
{
    public class AzureOpenAIHelper
    {
        public async static Task<string> LLMSummarization(string extractedData)
        {
            Uri azureOpenAIEndpoint = new(DocumentIntelligenceConfiguration.AzureOpenAIEndpoint);
            string azureOpenAIKey = DocumentIntelligenceConfiguration.AzureOpenAIKey;
            string azureOpenAIModel = DocumentIntelligenceConfiguration.AzureOpenAIModel;

            OpenAIClient client = new(azureOpenAIEndpoint, new AzureKeyCredential(azureOpenAIKey));

            ChatCompletionsOptions chatCompletionsOptions = new();
            string systemPrompt = "You are an expert in summarization, and you need to summarize the input data about a person in a natural way, including who they are, where they live, and other relevant information based on the provided data.";
            
            AddMessageToChat(chatCompletionsOptions, systemPrompt, ChatRole.System);
            AddMessageToChat(chatCompletionsOptions, extractedData, ChatRole.User);

            ChatCompletions response = await client.GetChatCompletionsAsync(azureOpenAIModel, chatCompletionsOptions);
            return response.Choices[0].Message.Content;
        }

        public static void AddMessageToChat(ChatCompletionsOptions options, string content, ChatRole role)
        {
            options.Messages.Add(new ChatMessage(role, content));
        }
    }
}
