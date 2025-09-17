using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Middleware;
using AzureDevOpsAI.Backend.Services;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Endpoints;

/// <summary>
/// Chat and AI endpoints.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>
    /// Configure chat endpoints.
    /// </summary>
    /// <param name="app">Web application builder</param>
    public static void MapChatEndpoints(this WebApplication app)
    {
        var chatGroup = app.MapGroup("/api/chat")
            .WithTags("chat")
            .WithOpenApi();

        // Apply conditional authorization based on DisableAuth setting
        chatGroup.MapPost("/message", SendMessageAsync)
            .WithName("SendMessage")
            .WithSummary("Send a chat message")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        chatGroup.MapGet("/conversations", GetConversationsAsync)
            .WithName("GetConversations")
            .WithSummary("Get user conversations")
            .Produces<List<Conversation>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        chatGroup.MapGet("/conversations/{conversationId}", GetConversationAsync)
            .WithName("GetConversation")
            .WithSummary("Get a specific conversation")
            .Produces<Conversation>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Send a chat message.
    /// </summary>
    private static async Task<IResult> SendMessageAsync(
        [FromBody] ChatRequest request,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        IAIService aiService)
    {
        try
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ChatEndpoints");

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = 400,
                        Message = "Message is required",
                        Type = "validation_error"
                    }
                });
            }

            // Get user ID (mock for now)
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Processing chat message for user {UserId}", userId);

            // Process message using AI service
            var response = await aiService.ProcessChatMessageAsync(
                request.Message, 
                request.ConversationId);

            logger.LogInformation("Successfully processed chat message for user {UserId}, conversation {ConversationId}", 
                userId, response.ConversationId);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ChatEndpoints");
            logger.LogError(ex, "Error processing chat message");
            
            return Results.Problem("An error occurred while processing the chat message. Please try again later.");
        }
    }

    /// <summary>
    /// Get user conversations.
    /// </summary>
    private static async Task<IResult> GetConversationsAsync(
        HttpContext context,
        IOptions<SecuritySettings> securitySettings)
    {
        try
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ChatEndpoints");
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Getting conversations for user {UserId}", userId);

            // Mock conversations (replace with actual data storage later)
            var conversations = new List<Conversation>();

            return Results.Ok(conversations);
        }
        catch (Exception ex)
        {
            // Log error (implementation simplified for initial conversion)
            return Results.Problem("An error occurred while getting conversations");
        }
    }

    /// <summary>
    /// Get a specific conversation.
    /// </summary>
    private static async Task<IResult> GetConversationAsync(
        string conversationId,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings)
    {
        try
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ChatEndpoints");
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Getting conversation {ConversationId} for user {UserId}", conversationId, userId);

            // Mock conversation (replace with actual data storage later)
            var conversation = new Conversation
            {
                Id = conversationId,
                UserId = userId,
                Title = "Mock Conversation",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            return Results.Ok(conversation);
        }
        catch (Exception ex)
        {
            // Log error (implementation simplified for initial conversion)
            return Results.Problem("An error occurred while getting the conversation");
        }
    }

    /// <summary>
    /// Get current user ID from context or use mock.
    /// </summary>
    private static string GetCurrentUserId(HttpContext context, SecuritySettings securitySettings)
    {
        if (securitySettings.DisableAuth)
        {
            return "mock-user";
        }

        // Extract user ID from JWT claims when authentication is enabled
        var userIdClaim = context.User?.FindFirst("sub") ?? context.User?.FindFirst("oid");
        return userIdClaim?.Value ?? "unknown-user";
    }
}