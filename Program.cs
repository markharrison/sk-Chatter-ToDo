﻿using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp.Models.Chat;
using System.Diagnostics.Contracts;

#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0003, SKEXP0011, SKEXP0020, SKEXP0050, SKEXP0052, SKEXP0055, SKEXP0011, SKEXP0010, SKEXP0070

namespace Chatter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Task.Run(() => { });

            Console.WriteLine("*** Chatter ***");
            Console.WriteLine("I am an assistant that manages your To-Do items");

            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development";
            var hostBuilder = Host.CreateApplicationBuilder(args);
            hostBuilder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();

            var configuration = hostBuilder.Configuration;

            string azopwnaiApikey = configuration["AzOpenAI:ApiKey"] ?? "";
            string azopwnaiEndpoint = configuration["AzOpenAI:Endpoint"] ?? "";
            string azopenaiCCDeploymentname = configuration["AzOpenAI:ChatCompletionDeploymentName"] ?? "";
            string azopenaiEmbeddingDeploymentname = configuration["AzOpenAI:EmbeddingDeploymentName"] ?? "";
            bool traceOn = bool.TryParse(configuration["TraceOn"], out bool tOn) && tOn;

            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.AddAzureOpenAIChatCompletion(azopenaiCCDeploymentname, azopwnaiEndpoint, azopwnaiApikey);
            kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);
            kernelBuilder.Plugins.AddFromType<ToDoPlugin>();

            var kernel = kernelBuilder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            ChatHistory chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage("You are a assistant - that manages list of To-Do items.");

            while (true)
            {
                Console.Write("Prompt: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                chatHistory.AddUserMessage(input);

                try
                {

                    var contents = chatService.GetStreamingChatMessageContentsAsync(
                                chatHistory,
                                new AzureOpenAIPromptExecutionSettings()
                                {
                                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                                },
                                kernel);

                    string fullContent = "";
                    await foreach (var content in contents)
                    {
                        Console.Write(content);
                        fullContent += content;
                    }
                    Console.Write("\n");

                    chatHistory.AddAssistantMessage(fullContent);

                    if (traceOn)
                    {
                        foreach (ChatMessageContent message in chatHistory)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($">> {message.Role}: {message.Content}  ");
                            if (message.Role.ToString() == "Assistant")
                            {
                                foreach (var item in message.Items)
                                {
                                    if (item is Microsoft.SemanticKernel.FunctionCallContent functionCallContent)
                                    {
                                        Console.WriteLine($"     {functionCallContent.FunctionName} ({functionCallContent.PluginName}) ");
                                    }
                                }
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;

                    int idx = chatHistory.Count - 1;
                    chatHistory.RemoveAt(idx);

                }

            }

        }

    }
}
