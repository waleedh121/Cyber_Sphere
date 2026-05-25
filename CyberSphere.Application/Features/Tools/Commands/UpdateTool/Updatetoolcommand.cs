using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Tools.Commands.SubmitTool;
using CyberSphere.Application.Features.Tools.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Tools.Commands.UpdateTool
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record UpdateToolCommand(
        Guid ToolId,
        string Name,
        string Description,
        Guid CategoryId,
        DifficultyLevel DifficultyLevel,
        string? GitHubUrl,
        List<string>? Tags
    ) : IRequest<ToolSubmissionResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class UpdateToolCommandValidator : AbstractValidator<UpdateToolCommand>
    {
        public UpdateToolCommandValidator()
        {
            RuleFor(x => x.ToolId)
                .NotEmpty().WithMessage("ToolId is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tool name is required.")
                .MinimumLength(3).WithMessage("Tool name must be at least 3 characters.")
                .MaximumLength(100).WithMessage("Tool name must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MinimumLength(20).WithMessage("Description must be at least 20 characters.")
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x.GitHubUrl)
                .MaximumLength(500).WithMessage("GitHub URL must not exceed 500 characters.")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                             && (u.Host == "github.com" || u.Host == "www.github.com"))
                .WithMessage("GitHub URL must be a valid github.com URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.GitHubUrl));

            RuleFor(x => x.Tags)
                .Must(t => t!.Count <= 10).WithMessage("A tool may have at most 10 tags.")
                .When(x => x.Tags is not null);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class UpdateToolCommandHandler : IRequestHandler<UpdateToolCommand, ToolSubmissionResponse>
    {
        private readonly IToolRepository _tools;
        private readonly ICategoryRepository _categories;
        private readonly ICurrentUserService _currentUser;

        public UpdateToolCommandHandler(
            IToolRepository tools,
            ICategoryRepository categories,
            ICurrentUserService currentUser)
        {
            _tools = tools;
            _categories = categories;
            _currentUser = currentUser;
        }

        public async Task<ToolSubmissionResponse> Handle(UpdateToolCommand cmd, CancellationToken ct)
        {
            var tool = await _tools.GetByIdWithDetailsAsync(cmd.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), cmd.ToolId);

            // Ownership check — only the owner may edit their tool
            if (tool.OwnerId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this tool.");

            // Editing is only allowed while Pending or Rejected
            // Approved tools are locked — they must be rejected first by an admin
            if (tool.Status == ToolStatus.Approved)
                throw new BadRequestException(
                    "Approved tools cannot be edited. Contact an admin to reopen the review.");

            var category = await _categories.GetByIdAsync(cmd.CategoryId, ct)
                ?? throw new NotFoundException(nameof(Category), cmd.CategoryId);

            // Name uniqueness check — exclude the tool being edited
            if (tool.Name != cmd.Name && await _tools.ExistsByNameAsync(cmd.Name, ct))
                throw new ConflictException($"A tool named '{cmd.Name}' already exists.");

            tool.UpdateDetails(
                categoryId: cmd.CategoryId,
                name: cmd.Name,
                description: cmd.Description,
                difficultyLevel: cmd.DifficultyLevel,
                gitHubUrl: cmd.GitHubUrl,
                tags: cmd.Tags);

            // If rejected, editing automatically resets to Pending (re-enters review queue)
            if (tool.Status == ToolStatus.Rejected)
                tool.ResetToPending();

            _tools.Update(tool);
            await _tools.SaveChangesAsync(ct);

            return SubmitToolCommandHandler.MapToResponse(tool, category.Name);
        }
    }

}
