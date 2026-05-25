using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Ai.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CyberSphere.Application.Features.Ai.Commands.SendMessage
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record SendAiMessageCommand(
        Guid SessionId,
        string Message
    ) : IRequest<AiChatResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class SendAiMessageCommandValidator : AbstractValidator<SendAiMessageCommand>
    {
        public SendAiMessageCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("SessionId is required.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MinimumLength(1).WithMessage("Message cannot be empty.")
                .MaximumLength(4000).WithMessage("Message must not exceed 4000 characters.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class SendAiMessageCommandHandler : IRequestHandler<SendAiMessageCommand, AiChatResponse>
    {
        private readonly IAiSessionRepository _sessions;
        private readonly IUserAiStatsRepository _stats;
        private readonly IAiGatewayService _aiGateway;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<SendAiMessageCommandHandler> _logger;

        // System prompt injected into every conversation — defines AI behaviour
        private const string SystemPrompt =
            "You are CyperSphere AI, an expert cybersecurity assistant. " +
            "Help users understand security concepts, suggest tools from the CyperSphere marketplace, " +
            "explain terminal commands, and assist with cybersecurity learning. " +
            "Be precise, practical, and security-focused. " +
            "Never assist with illegal or unethical hacking activities.";

        public SendAiMessageCommandHandler(
            IAiSessionRepository sessions,
            IUserAiStatsRepository stats,
            IAiGatewayService aiGateway,
            ICurrentUserService currentUser,
            ILogger<SendAiMessageCommandHandler> logger)
        {
            _sessions = sessions;
            _stats = stats;
            _aiGateway = aiGateway;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<AiChatResponse> Handle(SendAiMessageCommand cmd, CancellationToken ct)
        {
            // ── Load session with full history ────────────────────────────────────
            var session = await _sessions.GetByIdWithMessagesAsync(cmd.SessionId, ct)
                ?? throw new NotFoundException(nameof(AiSession), cmd.SessionId);

            if (session.UserId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this AI session.");

            if (!session.IsActive)
                throw new BadRequestException(
                    "This AI session has ended. Start a new session to continue chatting.");

            // ── Persist user message first (before calling AI) ────────────────────
            var userMessage = AiMessage.CreateUserMessage(session.Id, cmd.Message);
            session.Messages.Add(userMessage);

            // ── Build conversation context for the Python service ─────────────────
            // The Python service is stateless — we send the full history every time
            var context = BuildConversationContext(session);

            // ── Forward to Python AI microservice ─────────────────────────────────
            _logger.LogInformation(
                "Forwarding AI request for session {SessionId}, user {UserId}, message length {Length}",
                session.Id, _currentUser.UserId, cmd.Message.Length);

            var aiResponse = await _aiGateway.SendAsync(context, ct);

            // ── Create assistant message (error or success) ───────────────────────
            var assistantMessage = aiResponse.Success
                ? AiMessage.CreateAssistantMessage(
                    sessionId: session.Id,
                    content: aiResponse.Content,
                    tokensUsed: aiResponse.TokensUsed,
                    latencyMs: aiResponse.LatencyMs)
                : AiMessage.CreateErrorMessage(
                    sessionId: session.Id,
                    errorMessage: aiResponse.ErrorMessage ?? "Unknown error");

            session.Messages.Add(assistantMessage);

            // ── Update analytics — upsert stats row ───────────────────────────────
            var userStats = await _stats.GetByUserIdAsync(_currentUser.UserId, ct);
            if (userStats is null)
            {
                userStats = UserAiStats.CreateForUser(_currentUser.UserId);
                await _stats.AddAsync(userStats, ct);
            }

            userStats.RecordRequest(aiResponse.Success ? aiResponse.TokensUsed : 0);

            // ── Persist everything in one transaction ─────────────────────────────
            _sessions.Update(session);
            await _sessions.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AI response for session {SessionId}: success={Success}, tokens={Tokens}, latency={Latency}ms",
                session.Id, aiResponse.Success, aiResponse.TokensUsed, aiResponse.LatencyMs);

            return new AiChatResponse(
                SessionId: session.Id,
                UserMessage: MapMessage(userMessage),
                AssistantMessage: MapMessage(assistantMessage)
            );
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static IReadOnlyList<AiGatewayMessage> BuildConversationContext(AiSession session)
        {
            var messages = new List<AiGatewayMessage>
        {
            // System prompt is always first
            new("system", SystemPrompt)
        };

            // Append non-error history in chronological order
            var history = session.Messages
                .Where(m => !m.IsError)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AiGatewayMessage(
                    m.Role == AiMessageRole.User ? "user" : "assistant",
                    m.Content));

            messages.AddRange(history);
            return messages.AsReadOnly();
        }

        internal static AiMessageResponse MapMessage(AiMessage m) =>
            new(m.Id, m.Role.ToString(), m.Content, m.IsError, m.TokensUsed, m.LatencyMs, m.CreatedAt);
    }
}
