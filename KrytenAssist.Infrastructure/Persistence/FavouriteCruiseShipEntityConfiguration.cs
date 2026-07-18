using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class FavouriteCruiseShipEntityConfiguration : IEntityTypeConfiguration<FavouriteCruiseShipEntity>
{
    public void Configure(EntityTypeBuilder<FavouriteCruiseShipEntity> builder)
    {
        builder.ToTable("FavouriteCruiseShips", table =>
        {
            table.HasCheckConstraint("CK_FavouriteCruiseShips_Operator_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
            table.HasCheckConstraint("CK_FavouriteCruiseShips_Ship_Length", "length(\"ShipName\") BETWEEN 1 AND 500");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperatorId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShipName).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.OperatorId, x.ShipName }).IsUnique().HasDatabaseName("UX_FavouriteCruiseShips_Operator_Ship");
    }
}
