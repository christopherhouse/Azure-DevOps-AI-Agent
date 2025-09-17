using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Middleware;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Endpoints;

/// <summary>
/// Project management endpoints.
/// </summary>
public static class ProjectEndpoints
{
    /// <summary>
    /// Configure project endpoints.
    /// </summary>
    /// <param name="app">Web application builder</param>
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var projectGroup = app.MapGroup("/api/projects")
            .WithTags("projects")
            .WithOpenApi();

        projectGroup.MapGet("", GetProjectsAsync)
            .WithName("GetProjects")
            .WithSummary("Get projects")
            .Produces<ProjectList>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        projectGroup.MapPost("", CreateProjectAsync)
            .WithName("CreateProject")
            .WithSummary("Create a new project")
            .Produces<Project>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        projectGroup.MapGet("{projectId}", GetProjectAsync)
            .WithName("GetProject")
            .WithSummary("Get a specific project")
            .Produces<Project>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        projectGroup.MapPut("{projectId}", UpdateProjectAsync)
            .WithName("UpdateProject")
            .WithSummary("Update a project")
            .Produces<Project>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        projectGroup.MapDelete("{projectId}", DeleteProjectAsync)
            .WithName("DeleteProject")
            .WithSummary("Delete a project")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Get projects with pagination.
    /// </summary>
    private static async Task<IResult> GetProjectsAsync(
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        [FromServices] ILogger logger,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
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

            logger.LogInformation("Getting projects for user {UserId} (skip: {Skip}, limit: {Limit})", 
                userId, skip, limit);

            // Mock project list (replace with actual Azure DevOps integration later)
            var projects = new ProjectList
            {
                Projects = new List<Project>(),
                Count = 0
            };

            return Results.Ok(projects);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting projects");
            return Results.Problem("An error occurred while getting projects");
        }
    }

    /// <summary>
    /// Create a new project.
    /// </summary>
    private static async Task<IResult> CreateProjectAsync(
        [FromBody] ProjectCreate request,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        [FromServices] ILogger logger)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = 400,
                        Message = "Project name is required",
                        Type = "validation_error"
                    }
                });
            }

            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Creating project '{ProjectName}' for user {UserId}", request.Name, userId);

            // Mock project creation (replace with actual Azure DevOps integration later)
            var project = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                State = ProjectState.Created,
                Visibility = request.Visibility,
                Revision = 1,
                LastUpdateTime = DateTime.UtcNow
            };

            return Results.Created($"/api/projects/{project.Id}", project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating project");
            return Results.Problem("An error occurred while creating the project");
        }
    }

    /// <summary>
    /// Get a specific project.
    /// </summary>
    private static async Task<IResult> GetProjectAsync(
        string projectId,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        [FromServices] ILogger logger)
    {
        try
        {
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Getting project {ProjectId} for user {UserId}", projectId, userId);

            // Mock project (replace with actual Azure DevOps integration later)
            var project = new Project
            {
                Id = projectId,
                Name = "Mock Project",
                Description = "A mock project for testing",
                State = ProjectState.Created,
                Visibility = ProjectVisibility.Private,
                Revision = 1,
                LastUpdateTime = DateTime.UtcNow
            };

            return Results.Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting project {ProjectId}", projectId);
            return Results.Problem("An error occurred while getting the project");
        }
    }

    /// <summary>
    /// Update a project.
    /// </summary>
    private static async Task<IResult> UpdateProjectAsync(
        string projectId,
        [FromBody] ProjectUpdate request,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        [FromServices] ILogger logger)
    {
        try
        {
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Updating project {ProjectId} for user {UserId}", projectId, userId);

            // Mock project update (replace with actual Azure DevOps integration later)
            var project = new Project
            {
                Id = projectId,
                Name = request.Name ?? "Updated Project",
                Description = request.Description ?? "Updated description",
                State = ProjectState.Created,
                Visibility = ProjectVisibility.Private,
                Revision = 2,
                LastUpdateTime = DateTime.UtcNow
            };

            return Results.Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating project {ProjectId}", projectId);
            return Results.Problem("An error occurred while updating the project");
        }
    }

    /// <summary>
    /// Delete a project.
    /// </summary>
    private static async Task<IResult> DeleteProjectAsync(
        string projectId,
        HttpContext context,
        IOptions<SecuritySettings> securitySettings,
        [FromServices] ILogger logger)
    {
        try
        {
            var userId = GetCurrentUserId(context, securitySettings.Value);

            logger.LogInformation("Deleting project {ProjectId} for user {UserId}", projectId, userId);

            // Mock project deletion (replace with actual Azure DevOps integration later)
            
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
            return Results.Problem("An error occurred while deleting the project");
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