using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Tools.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using CyberSphere.Application.Exceptions;

namespace CyberSphere.Application.Features.Tools.Commands.SubmitTool
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record SubmitToolCommand(
        string Name,
        string Description,
        Guid CategoryId,
        DifficultyLevel DifficultyLevel,
        string? GitHubUrl,
        List<string>? Tags,
        List<SubmitCommandRequest>? Commands
    ) : IRequest<ToolSubmissionResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class SubmitToolCommandValidator : AbstractValidator<SubmitToolCommand>
    {
        public SubmitToolCommandValidator()
        {
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

            RuleForEach(x => x.Tags)
                .MaximumLength(30).WithMessage("Each tag must not exceed 30 characters.")
                .When(x => x.Tags is not null);

            RuleFor(x => x.Commands)
                .Must(c => c!.Count <= 30).WithMessage("A tool may have at most 30 commands.")
                .When(x => x.Commands is not null);

            RuleForEach(x => x.Commands)
                .ChildRules(cmd =>
                {
                    cmd.RuleFor(c => c.Name)
                        .NotEmpty().WithMessage("Command name is required.")
                        .MaximumLength(100).WithMessage("Command name must not exceed 100 characters.");
                    cmd.RuleFor(c => c.Description)
                        .NotEmpty().WithMessage("Command description is required.")
                        .MaximumLength(1000).WithMessage("Command description must not exceed 1000 characters.");
                    cmd.RuleFor(c => c.Syntax)
                        .NotEmpty().WithMessage("Command syntax is required.")
                        .MaximumLength(500).WithMessage("Command syntax must not exceed 500 characters.");
                    cmd.RuleFor(c => c.Example)
                        .MaximumLength(1000).WithMessage("Command example must not exceed 1000 characters.")
                        .When(c => c.Example is not null);
                })
                .When(x => x.Commands is not null);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class SubmitToolCommandHandler : IRequestHandler<SubmitToolCommand, ToolSubmissionResponse>
    {
        private readonly IToolRepository _tools;
        private readonly ICategoryRepository _categories;
        private readonly ICurrentUserService _currentUser;

        public SubmitToolCommandHandler(
            IToolRepository tools,
            ICategoryRepository categories,
            ICurrentUserService currentUser)
        {
            _tools = tools;
            _categories = categories;
            _currentUser = currentUser;
        }

        public async Task<ToolSubmissionResponse> Handle(SubmitToolCommand cmd, CancellationToken ct)
        {
            // Validate category exists
            var category = await _categories.GetByIdAsync(cmd.CategoryId, ct)
                ?? throw new NotFoundException(nameof(Category), cmd.CategoryId);

            // Prevent duplicate tool names across the platform
            if (await _tools.ExistsByNameAsync(cmd.Name, ct))
                throw new ConflictException($"A tool named '{cmd.Name}' already exists.");

            // Create the aggregate root
            var tool = Tool.Create(
                ownerId: _currentUser.UserId,
                categoryId: cmd.CategoryId,
                name: cmd.Name,
                description: cmd.Description,
                difficultyLevel: cmd.DifficultyLevel,
                gitHubUrl: cmd.GitHubUrl,
                tags: cmd.Tags);

            // Attach commands to the tool (in-memory before SaveChanges)
            if (cmd.Commands is { Count: > 0 })
            {
                foreach (var c in cmd.Commands)
                {
                    var command = ToolCommand.Create(
                        toolId: tool.Id,
                        name: c.Name,
                        description: c.Description,
                        syntax: c.Syntax,
                        example: c.Example);

                    tool.Commands.Add(command);
                }
            }

            await _tools.AddAsync(tool, ct);
            await _tools.SaveChangesAsync(ct);

            return MapToResponse(tool, category.Name);
        }

        // ── Mapping ───────────────────────────────────────────────────────────────

        internal static ToolSubmissionResponse MapToResponse(Tool tool, string categoryName) =>
            new(
                Id: tool.Id,
                Name: tool.Name,
                Description: tool.Description,
                CategoryName: categoryName,
                DifficultyLevel: tool.DifficultyLevel.ToString(),
                Status: tool.Status.ToString(),
                GitHubUrl: tool.GitHubUrl,
                Tags: tool.GetTags(),
                Commands: tool.Commands
                                     .Select(c => new ToolCommandResponse(c.Id, c.Name, c.Description, c.Syntax, c.Example))
                                     .ToList()
                                     .AsReadOnly(),
                CreatedAt: tool.CreatedAt
            );
    }

}
