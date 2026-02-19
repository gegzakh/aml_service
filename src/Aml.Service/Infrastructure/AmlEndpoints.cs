using AmlOps.Backend.Application;
using AmlOps.Backend.Application.Contracts;

namespace AmlOps.Backend.Infrastructure;

public static class AmlEndpoints
{
    public static void MapAmlEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var response = await authService.LoginAsync(request, ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        var api = app.MapGroup("/api").RequireAuthorization();

        var cases = api.MapGroup("/cases");
        cases.MapGet("", async (ICaseService service, CancellationToken ct, string? status = null, Guid? owner = null, bool overdue = false) =>
        {
            var result = await service.GetCasesAsync(status, owner, overdue, ct);
            return Results.Ok(result);
        });

        cases.MapPost("", async (CreateCaseRequest request, ICaseService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerFullName))
            {
                return Results.BadRequest(new { message = "Customer full name is required." });
            }

            var created = await service.CreateCaseAsync(request, ct);
            return Results.Created($"/api/cases/{created.Id}", created);
        });

        cases.MapGet("/{id:guid}", async (Guid id, ICaseService service, CancellationToken ct) =>
        {
            var item = await service.GetCaseAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        cases.MapPost("/{id:guid}/assign", async (Guid id, AssignCaseRequest request, ICaseService service, CancellationToken ct) =>
        {
            var item = await service.AssignCaseAsync(id, request, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        cases.MapPost("/{id:guid}/status", async (Guid id, UpdateCaseStatusRequest request, ICaseService service, CancellationToken ct) =>
        {
            var item = await service.UpdateStatusAsync(id, request, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        cases.MapPost("/{id:guid}/decision", async (Guid id, SetDecisionRequest request, ICaseService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Decision) || string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.BadRequest(new { message = "Decision and reason are required." });
            }

            var item = await service.SetDecisionAsync(id, request, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        cases.MapPost("/{id:guid}/approve", async (Guid id, ICaseService service, CancellationToken ct) =>
        {
            var item = await service.ApproveCaseAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        cases.MapPost("/{id:guid}/close", async (Guid id, ICaseService service, CancellationToken ct) =>
        {
            try
            {
                var item = await service.CloseCaseAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        cases.MapGet("/{id:guid}/timeline", async (Guid id, ICaseService service, CancellationToken ct) =>
            Results.Ok(await service.GetTimelineAsync(id, ct)));

        cases.MapPost("/{id:guid}/comments", async (Guid id, AddCommentRequest request, ICaseService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new { message = "Comment text is required." });
            }

            var comment = await service.AddCommentAsync(id, request, ct);
            return comment is null ? Results.NotFound() : Results.Ok(comment);
        });

        cases.MapGet("/{id:guid}/comments", async (Guid id, ICaseService service, CancellationToken ct) =>
            Results.Ok(await service.GetCommentsAsync(id, ct)));

        cases.MapPost("/{id:guid}/attachments/presign", (Guid id) =>
        {
            var fileKey = $"cases/{id}/{Guid.NewGuid()}";
            return Results.Ok(new { fileKey, uploadUrl = $"mock://upload/{fileKey}", expiresInSeconds = 900 });
        });

        cases.MapPost("/{id:guid}/attachments/complete", async (Guid id, CompleteAttachmentRequest request, ICaseService service, CancellationToken ct) =>
        {
            var attachment = await service.AddAttachmentAsync(id, request, ct);
            return attachment is null ? Results.NotFound() : Results.Ok(attachment);
        });

        cases.MapGet("/{id:guid}/attachments", async (Guid id, ICaseService service, CancellationToken ct) =>
            Results.Ok(await service.GetAttachmentsAsync(id, ct)));

        cases.MapGet("/{id:guid}/export/evidence-pack", async (Guid id, IEvidencePackService service, CancellationToken ct) =>
        {
            var bytes = await service.GeneratePdfAsync(id, ct);
            return Results.File(bytes, "application/pdf", $"evidence-pack-{id}.pdf");
        });

        api.MapGet("/attachments/{id:guid}/download", (Guid id) =>
            Results.Ok(new { attachmentId = id, signedUrl = $"mock://download/{id}" }));

        api.MapPost("/import/alerts/csv", async (IFormFile file, IImportService service, CancellationToken ct) =>
        {
            if (file.Length == 0)
            {
                return Results.BadRequest(new { message = "CSV file is empty." });
            }

            await using var stream = file.OpenReadStream();
            var result = await service.ImportAlertsCsvAsync(stream, ct);
            return Results.Ok(result);
        }).DisableAntiforgery();

        api.MapGet("/dashboard", async (ICaseService service, CancellationToken ct) =>
            Results.Ok(await service.GetDashboardAsync(ct)));

        var admin = api.MapGroup("/admin");
        admin.MapGet("/sla-settings", async (IAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetSlaSettingsAsync(ct)));
        admin.MapPost("/sla-settings", async (UpdateSlaSettingsRequest request, IAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateSlaSettingsAsync(request, ct)));
    }
}
