using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Azure.Identity;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Models;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for Cosmos DB persistence operations.
/// </summary>
public interface ICosmosDbService
{
    /// <summary>
    /// Get chat history for a conversation.
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat history document or null if not found</returns>
    Task<ChatHistoryDocument?> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save or update chat history for a conversation.
    /// </summary>
    /// <param name="chatHistory">Chat history document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveChatHistoryAsync(ChatHistoryDocument chatHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get thought process by ID.
    /// </summary>
    /// <param name="thoughtProcessId">Thought process ID</param>
    /// <param name="conversationId">Conversation ID (partition key)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thought process document or null if not found</returns>
    Task<ThoughtProcessDocument?> GetThoughtProcessAsync(string thoughtProcessId, string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save or update thought process.
    /// </summary>
    /// <param name="thoughtProcess">Thought process document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveThoughtProcessAsync(ThoughtProcessDocument thoughtProcess, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cosmos DB service implementation for chat history and thought process persistence.
/// </summary>
public class CosmosDbService : ICosmosDbService, IAsyncDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _chatHistoryContainer;
    private readonly Container _thoughtProcessContainer;
    private readonly ILogger<CosmosDbService> _logger;
    private readonly CosmosDbSettings _settings;

    public CosmosDbService(IOptions<CosmosDbSettings> settings, ILogger<CosmosDbService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Create CosmosClient with managed identity
        var clientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        if (_settings.UseManagedIdentity)
        {
            // Use managed identity authentication
            Azure.Core.TokenCredential credential;
            
            if (!string.IsNullOrEmpty(_settings.ClientId))
            {
                // Use User Assigned Managed Identity
                credential = new ManagedIdentityCredential(_settings.ClientId);
                _logger.LogInformation("Initializing CosmosDB with User Assigned Managed Identity: {ClientId}", _settings.ClientId);
            }
            else
            {
                // Use System Assigned Managed Identity
                credential = new ManagedIdentityCredential();
                _logger.LogInformation("Initializing CosmosDB with System Assigned Managed Identity");
            }

            _cosmosClient = new CosmosClient(_settings.Endpoint, credential, clientOptions);
        }
        else
        {
            // For local development without managed identity, this would fail
            // In production, managed identity should always be used
            throw new InvalidOperationException("Cosmos DB requires managed identity authentication. Set UseManagedIdentity to true.");
        }

        // Get database and containers
        var database = _cosmosClient.GetDatabase(_settings.DatabaseName);
        _chatHistoryContainer = database.GetContainer(_settings.ChatHistoryContainerName);
        _thoughtProcessContainer = database.GetContainer(_settings.ThoughtProcessContainerName);

        _logger.LogInformation(
            "CosmosDB service initialized - Endpoint: {Endpoint}, Database: {Database}, Containers: {ChatHistory}, {ThoughtProcess}",
            _settings.Endpoint,
            _settings.DatabaseName,
            _settings.ChatHistoryContainerName,
            _settings.ThoughtProcessContainerName);
    }

    /// <summary>
    /// Get chat history for a conversation.
    /// </summary>
    public async Task<ChatHistoryDocument?> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _chatHistoryContainer.ReadItemAsync<ChatHistoryDocument>(
                conversationId,
                new PartitionKey(conversationId),
                cancellationToken: cancellationToken);

            _logger.LogDebug("Retrieved chat history for conversation {ConversationId}, message count: {Count}",
                conversationId, response.Resource.Messages.Count);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Chat history not found for conversation {ConversationId}", conversationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Save or update chat history for a conversation.
    /// </summary>
    public async Task SaveChatHistoryAsync(ChatHistoryDocument chatHistory, CancellationToken cancellationToken = default)
    {
        try
        {
            chatHistory.UpdatedAt = DateTime.UtcNow;

            await _chatHistoryContainer.UpsertItemAsync(
                chatHistory,
                new PartitionKey(chatHistory.ConversationId),
                cancellationToken: cancellationToken);

            _logger.LogDebug("Saved chat history for conversation {ConversationId}, message count: {Count}",
                chatHistory.ConversationId, chatHistory.Messages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chat history for conversation {ConversationId}", chatHistory.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Get thought process by ID.
    /// </summary>
    public async Task<ThoughtProcessDocument?> GetThoughtProcessAsync(string thoughtProcessId, string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _thoughtProcessContainer.ReadItemAsync<ThoughtProcessDocument>(
                thoughtProcessId,
                new PartitionKey(conversationId),
                cancellationToken: cancellationToken);

            _logger.LogDebug("Retrieved thought process {ThoughtProcessId} for conversation {ConversationId}",
                thoughtProcessId, conversationId);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Thought process {ThoughtProcessId} not found for conversation {ConversationId}",
                thoughtProcessId, conversationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thought process {ThoughtProcessId} for conversation {ConversationId}",
                thoughtProcessId, conversationId);
            throw;
        }
    }

    /// <summary>
    /// Save or update thought process.
    /// </summary>
    public async Task SaveThoughtProcessAsync(ThoughtProcessDocument thoughtProcess, CancellationToken cancellationToken = default)
    {
        try
        {
            await _thoughtProcessContainer.UpsertItemAsync(
                thoughtProcess,
                new PartitionKey(thoughtProcess.ConversationId),
                cancellationToken: cancellationToken);

            _logger.LogDebug("Saved thought process {ThoughtProcessId} for conversation {ConversationId}",
                thoughtProcess.Id, thoughtProcess.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving thought process {ThoughtProcessId} for conversation {ConversationId}",
                thoughtProcess.Id, thoughtProcess.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Dispose the Cosmos client.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _cosmosClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
