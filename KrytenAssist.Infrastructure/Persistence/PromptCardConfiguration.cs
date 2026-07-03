using System.Text.Json;
using KrytenAssist.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class PromptCardConfiguration : IEntityTypeConfiguration<PromptCard>
{
    public void Configure(EntityTypeBuilder<PromptCard> builder)
    {
        var tagsConverter = new ValueConverter<IReadOnlyCollection<string>, string>(
            tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<IReadOnlyCollection<string>>(json, (JsonSerializerOptions?)null)
                    ?? Array.Empty<string>());
        
        var tagsComparer = new ValueComparer<IReadOnlyCollection<string>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            tags => tags.Aggregate(0, (hashCode, tag) => HashCode.Combine(hashCode, tag.GetHashCode())),
            tags => tags.ToArray());

        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, string>(
            dateTimeOffset => dateTimeOffset.ToString("O"),
            value => DateTimeOffset.Parse(value));
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired();

        builder.Property(x => x.Category)
            .IsRequired();

        builder.Property(x => x.Description);

        builder.Property(x => x.PromptText)
            .IsRequired();

        var tagsProperty = builder.Property(x => x.Tags);

        tagsProperty
            .HasConversion(tagsConverter)
            .Metadata.SetValueComparer(tagsComparer);

        tagsProperty.IsRequired(); 

        builder.Property(x => x.CreatedAt)
            .HasConversion(dateTimeOffsetConverter)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasConversion(dateTimeOffsetConverter)
            .IsRequired();
    }
}