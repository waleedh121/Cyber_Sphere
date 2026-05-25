using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Ai.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Ai.Commands.StartSession
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record StartAiSessionCommand(string? Title) : IRequest<AiSessionResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class StartAiSessionCommandValidator : AbstractValidator<StartAiSessionCommand>
    {
        public StartAiSessionCommandValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(100).WithMessage("Session title must not exceed 100 characters.")
                .When(x => x.Title is not null);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class StartAiSessionCommandHandler
        : IRequestHandler<StartAiSessionCommand, AiSessionResponse>
    {
        private readonly IAiSessionRepository _sessions;
        private readonly IUserAiStatsRepository _stats;
        private readonly ICurrentUserService _currentUser;

        public StartAiSessionCommandHandler(
            IAiSessionRepository sessions,
            IUserAiStatsRepository stats,
            ICurrentUserService currentUser)
        {
            _sessions = sessions;
            _stats = stats;
            _currentUser = currentUser;
        }

        public async Task<AiSessionResponse> Handle(StartAiSessionCommand cmd, CancellationToken ct)
        {
            var session = AiSession.Create(
                userId: _currentUser.UserId,
                title: cmd.Title ?? "New AI Chat");

            await _sessions.AddAsync(session, ct);

            // Upsert analytics — create row on first use, increment otherwise
            var userStats = await _stats.GetByUserIdAsync(_currentUser.UserId, ct);
            if (userStats is null)
            {
                userStats = UserAiStats.CreateForUser(_currentUser.UserId);
                await _stats.AddAsync(userStats, ct);
            }

            userStats.RecordSessionStarted();

            await _sessions.SaveChangesAsync(ct);

            return MapToResponse(session, messageCount: 0);
        }

        internal static AiSessionResponse MapToResponse(AiSession s, int messageCount) =>
            new(s.Id, s.Title, s.Status.ToString(), s.StartedAt, s.EndedAt, messageCount);
    }
}
