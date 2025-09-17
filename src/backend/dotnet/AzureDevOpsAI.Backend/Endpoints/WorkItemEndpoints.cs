using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Middleware;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Endpoints;

/// <summary>
/// Work item management endpoints.
/// </summary>
public static class WorkItemEndpoints
{
    /// <summary>
    /// Configure work item endpoints.
    /// </summary>
    /// <param name="app">Web application builder</param>
    public static void MapWorkItemEndpoints(this WebApplication app)
    {
        var workItemGroup = app.MapGroup("/api/{projectId}/workitems")
            .WithTags("workitems")
            .WithOpenApi();

        workItemGroup.MapGet("", GetWorkItemsAsync)
            .WithName("GetWorkItems")
            .WithSummary("Get work items for a project")
            .Produces<WorkItemList>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        workItemGroup.MapPost("", CreateWorkItemAsync)
            .WithName("CreateWorkItem")
            .WithSummary("Create a new work item")
            .Produces<WorkItem>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        workItemGroup.MapGet("{workItemId:int}", GetWorkItemAsync)
            .WithName("GetWorkItem")
            .WithSummary("Get a specific work item")
            .Produces<WorkItem>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        workItemGroup.MapPut("{workItemId:int}", UpdateWorkItemAsync)
            .WithName("UpdateWorkItem")
            .WithSummary("Update a work item")
            .Produces<WorkItem>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        workItemGroup.MapDelete("{workItemId:int}", DeleteWorkItemAsync)
            .WithName("DeleteWorkItem")
            .WithSummary("Delete a work item")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Get work items for a project with filtering and pagination.
    /// </summary>
    private static async Task<IResult> GetWorkItemsAsync(
        string projectId,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100,
        [FromQuery] string? workItemType = null,
        [FromQuery] string? state = null)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WorkItemEndpoints");
        
        try
        {
            // Validate parameters
            if (skip < 0)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = 400,
                        Message = "Skip parameter must be non-negative",
                        Type = "validation_error"
                    }
                });
            }

            if (limit < 1 || limit > 1000)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = 400,
                        Message = "Limit parameter must be between 1 and 1000",
                        Type = "validation_error"
                    }
                });
            }

            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Getting work items for project {ProjectId} by user {UserId} " +
                "(skip: {Skip}, limit: {Limit}, type: {WorkItemType}, state: {State})", 
                projectId, userId, skip, limit, workItemType, state);

            // Mock work item list (replace with actual Azure DevOps integration later)
            var workItems = new WorkItemList
            {
                WorkItems = new List<WorkItem>(),
                Count = 0
            };

            return Results.Ok(workItems);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting work items for project {ProjectId}", projectId);
            return Results.Problem("An error occurred while getting work items");
        }
    }

    /// <summary>
    /// Create a new work item.
    /// </summary>
    private static async Task<IResult> CreateWorkItemAsync(
        string projectId,
        [FromBody] WorkItemCreate request,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WorkItemEndpoints");
        
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = 400,
                        Message = "Work item title is required",
                        Type = "validation_error"
                    }
                });
            }

            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Creating work item '{Title}' in project {ProjectId} for user {UserId}", 
                request.Title, projectId, userId);

            // Mock work item creation (replace with actual Azure DevOps integration later)
            var workItem = new WorkItem
            {
                Id = Random.Shared.Next(1000, 9999), // Mock ID
                Title = request.Title,
                WorkItemType = request.WorkItemType.ToString(),
                State = "New",
                Description = request.Description,
                AssignedTo = request.AssignedTo,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                ChangedDate = DateTime.UtcNow,
                Priority = request.Priority,
                Tags = request.Tags,
                ProjectId = projectId,
                ParentId = request.ParentId
            };

            return Results.Created($"/api/{projectId}/workitems/{workItem.Id}", workItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating work item in project {ProjectId}", projectId);
            return Results.Problem("An error occurred while creating the work item");
        }
    }

    /// <summary>
    /// Get a specific work item.
    /// </summary>
    private static async Task<IResult> GetWorkItemAsync(
        string projectId,
        int workItemId,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WorkItemEndpoints");
        
        try
        {
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Getting work item {WorkItemId} from project {ProjectId} for user {UserId}", 
                workItemId, projectId, userId);

            // Mock work item (replace with actual Azure DevOps integration later)
            var workItem = new WorkItem
            {
                Id = workItemId,
                Title = "Mock Work Item",
                WorkItemType = "User Story",
                State = "New",
                Description = "A mock work item for testing",
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ChangedDate = DateTime.UtcNow,
                Priority = 2,
                ProjectId = projectId
            };

            return Results.Ok(workItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting work item {WorkItemId} from project {ProjectId}", 
                workItemId, projectId);
            return Results.Problem("An error occurred while getting the work item");
        }
    }

    /// <summary>
    /// Update a work item.
    /// </summary>
    private static async Task<IResult> UpdateWorkItemAsync(
        string projectId,
        int workItemId,
        [FromBody] WorkItemUpdate request,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WorkItemEndpoints");
        
        try
        {
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Updating work item {WorkItemId} in project {ProjectId} for user {UserId}", 
                workItemId, projectId, userId);

            // Mock work item update (replace with actual Azure DevOps integration later)
            var workItem = new WorkItem
            {
                Id = workItemId,
                Title = request.Title ?? "Updated Work Item",
                WorkItemType = "User Story",
                State = request.State?.ToString() ?? "Active",
                Description = request.Description ?? "Updated description",
                AssignedTo = request.AssignedTo,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ChangedDate = DateTime.UtcNow,
                Priority = request.Priority ?? 2,
                Tags = request.Tags,
                ProjectId = projectId
            };

            return Results.Ok(workItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating work item {WorkItemId} in project {ProjectId}", 
                workItemId, projectId);
            return Results.Problem("An error occurred while updating the work item");
        }
    }

    /// <summary>
    /// Delete a work item.
    /// </summary>
    private static async Task<IResult> DeleteWorkItemAsync(
        string projectId,
        int workItemId,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WorkItemEndpoints");
        
        try
        {
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Deleting work item {WorkItemId} from project {ProjectId} for user {UserId}", 
                workItemId, projectId, userId);

            // Mock work item deletion (replace with actual Azure DevOps integration later)
            
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting work item {WorkItemId} from project {ProjectId}", 
                workItemId, projectId);
            return Results.Problem("An error occurred while deleting the work item");
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