﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RX.Nyss.Data.Models.Maps
{
    public class ProjectHealthRiskMap : IEntityTypeConfiguration<ProjectHealthRisk>
    {
        public void Configure(EntityTypeBuilder<ProjectHealthRisk> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Project).WithMany().IsRequired().OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.HealthRisk).WithMany().IsRequired().OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.AlertRule).WithMany().OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.FeedbackMessage).HasMaxLength(160);
        }
    }
}
