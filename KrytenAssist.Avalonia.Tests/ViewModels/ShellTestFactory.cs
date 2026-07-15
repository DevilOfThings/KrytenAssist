using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Options;
using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

internal static class ShellTestFactory
{
    internal static readonly SkillManifest FirstManifest = new(
        "test.first",
        "First Skill",
        "First deterministic test Skill.",
        "1.0.0");

    internal static readonly SkillManifest SecondManifest = new(
        "test.second",
        "Second Skill",
        "Second deterministic test Skill.",
        "2.0.0");

    internal static MainWindowViewModel CreateAssistantWorkspace()
    {
        return new MainWindowViewModel(
            new EmptyPromptCardStore(),
            new DeterministicEmbeddingService(),
            new CosineSimilarityService(),
            new DeterministicConversationService(),
            new EmptyConversationMemory(),
            Microsoft.Extensions.Options.Options.Create(new ConversationOptions()));
    }

    internal static SkillRegistry CreateRegistry(params CountingSkill[] skills)
    {
        var registry = new SkillRegistry();

        foreach (var skill in skills)
        {
            registry.Register(skill);
        }

        return registry;
    }

    internal static CruiseOfTheWeekViewModel CreateCruiseViewModel(
        ISkillRegistry? registry = null,
        IClock? clock = null)
    {
        return new CruiseOfTheWeekViewModel(
            registry ?? new SkillRegistry(),
            clock ?? new FixedClock());
    }

    internal static void AddAssistantDependencies(IServiceCollection services)
    {
        services.AddSingleton<IPromptCardStore, EmptyPromptCardStore>();
        services.AddSingleton<IEmbeddingService, DeterministicEmbeddingService>();
        services.AddSingleton<CosineSimilarityService>();
        services.AddSingleton<IConversationService, DeterministicConversationService>();
        services.AddSingleton<IConversationMemory, EmptyConversationMemory>();
        services.AddSingleton<FixedClock>();
        services.AddSingleton<IClock>(provider => provider.GetRequiredService<FixedClock>());
        services.AddSingleton<IOptions<ConversationOptions>>(
            Microsoft.Extensions.Options.Options.Create(new ConversationOptions()));
    }

    internal sealed class CountingSkill(SkillManifest manifest) : ISkill
    {
        public SkillManifest Manifest { get; } = manifest;

        public int ExecutionCount { get; private set; }

        public Task<SkillResult> ExecuteAsync(
            SkillRequest request,
            SkillContext context,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(SkillResult.Success("deterministic"));
        }
    }

    internal sealed class FixedClock : IClock
    {
        public DateTimeOffset NowValue { get; set; } =
            new(2026, 7, 14, 10, 30, 0, TimeSpan.FromHours(1));

        public int ReadCount { get; private set; }

        public DateTimeOffset Now
        {
            get
            {
                ReadCount++;
                return NowValue;
            }
        }
    }

    internal sealed class EmptyPromptCardStore : IPromptCardStore
    {
        public Task<IReadOnlyCollection<PromptCardModel>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyCollection<PromptCardModel>>([]);
        }

        public Task SaveAllAsync(IReadOnlyCollection<PromptCardModel> promptCards)
        {
            return Task.CompletedTask;
        }
    }

    internal sealed class DeterministicEmbeddingService : IEmbeddingService
    {
        public Task<EmbeddingVector> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmbeddingVector([1.0]));
        }
    }

    internal sealed class DeterministicConversationService : IConversationService
    {
        public Task<ConversationResponse> SendAsync(
            ConversationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConversationResponse { Content = "Response" });
        }
    }

    internal sealed class EmptyConversationMemory : IConversationMemory
    {
        public IReadOnlyList<ConversationMessage> GetRecentMessages() => [];

        public void AddTurn(
            ConversationMessage userMessage,
            ConversationMessage assistantMessage)
        {
        }

        public void Clear()
        {
        }
    }
}
