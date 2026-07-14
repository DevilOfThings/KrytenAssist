using FluentAssertions;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;

namespace KrytenAssist.Avalonia.Tests.Skills;

public sealed class SkillRegistryTests
{
    [Fact]
    public void Register_ShouldExposeRegisteredSkill()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new TestSkill("sample.test");

        // Act
        registry.Register(skill);

        // Assert
        registry.Skills.Should().ContainSingle()
            .Which.Should().BeSameAs(skill);
    }

    [Fact]
    public void Find_ShouldReturnRegisteredSkill()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new TestSkill("sample.test");
        registry.Register(skill);

        // Act
        var result = registry.Find("sample.test");

        // Assert
        result.Should().BeSameAs(skill);
    }

    [Fact]
    public void Find_ShouldBeCaseInsensitive()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new TestSkill("sample.test");
        registry.Register(skill);

        // Act
        var result = registry.Find("SAMPLE.TEST");

        // Assert
        result.Should().BeSameAs(skill);
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenSkillIsUnknown()
    {
        // Arrange
        var registry = new SkillRegistry();

        // Act
        var result = registry.Find("sample.unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldThrow_WhenSkillIdIsDuplicated()
    {
        // Arrange
        var registry = new SkillRegistry();
        registry.Register(new TestSkill("sample.test"));

        // Act
        Action act = () => registry.Register(new TestSkill("sample.test"));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Register_ShouldThrow_WhenSkillIdDiffersOnlyByCase()
    {
        // Arrange
        var registry = new SkillRegistry();
        registry.Register(new TestSkill("sample.test"));

        // Act
        Action act = () => registry.Register(new TestSkill("SAMPLE.TEST"));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Skills_ShouldPreserveRegistrationOrder()
    {
        // Arrange
        var registry = new SkillRegistry();
        var firstSkill = new TestSkill("sample.first");
        var secondSkill = new TestSkill("sample.second");
        var thirdSkill = new TestSkill("sample.third");

        // Act
        registry.Register(firstSkill);
        registry.Register(secondSkill);
        registry.Register(thirdSkill);

        // Assert
        registry.Skills.Should().ContainInOrder(
            firstSkill,
            secondSkill,
            thirdSkill);
    }

    [Fact]
    public void Skills_ShouldExposeRegisteredSkillManifest()
    {
        // Arrange
        var manifest = new SkillManifest(
            "sample.test",
            "Test",
            "A test Skill.",
            "1.2.3");
        var registry = new SkillRegistry();
        registry.Register(new TestSkill(manifest));

        // Act
        var result = registry.Skills.Single().Manifest;

        // Assert
        result.Should().BeSameAs(manifest);
    }

    [Fact]
    public void Register_ShouldThrow_WhenSkillIsNull()
    {
        // Arrange
        var registry = new SkillRegistry();

        // Act
        Action act = () => registry.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Find_ShouldThrow_WhenIdIsNull()
    {
        // Arrange
        var registry = new SkillRegistry();

        // Act
        Action act = () => registry.Find(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TestSkill : ISkill
    {
        public TestSkill(string id)
            : this(new SkillManifest(id, "Test", "A test Skill.", "1.0.0"))
        {
        }

        public TestSkill(SkillManifest manifest)
        {
            Manifest = manifest;
        }

        public SkillManifest Manifest { get; }

        public Task<SkillResult> ExecuteAsync(
            SkillRequest request,
            SkillContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SkillResult.Success());
        }
    }
}
