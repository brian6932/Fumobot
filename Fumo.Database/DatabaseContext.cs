﻿using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Fumo.Database.DTO;
using Microsoft.EntityFrameworkCore;

namespace Fumo.Database;

public class DatabaseContext : DbContext
{
    public DbSet<ChannelDTO> Channels { get; set; }

    public DbSet<UserDTO> Users { get; set; }

    // For migrations
    public DatabaseContext()
    { }

    public DatabaseContext(DbContextOptions<DatabaseContext> ctx)
        : base(ctx)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // It will already be configured from AutoFac during the normal run. This is only false when it generates migrations.
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=fumo;Username=fumo;Password=fumo");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelDTO>()
            .Property(x => x.DateJoined)
            .HasDefaultValueSql("now()");

        modelBuilder.Entity<ChannelDTO>()
            .Property(x => x.Settings)
            .HasDefaultValueSql("'[]'::jsonb");

        modelBuilder.Entity<ChannelDTO>()
            .Property(x => x.SetForDeletion)
            .HasDefaultValue(false);

        modelBuilder.Entity<UserDTO>()
            .Property(x => x.DateSeen)
            .HasDefaultValueSql("now()");

        modelBuilder.Entity<UserDTO>()
            .Property(x => x.Settings)
            .HasDefaultValueSql("'[]'::jsonb");

        modelBuilder.Entity<UserDTO>()
            .Property(x => x.UsernameHistory)
            .HasDefaultValueSql("'{}'::text[]");

        modelBuilder.Entity<UserDTO>()
            .Property(x => x.Permissions)
            .HasDefaultValueSql("'{\"default\"}'::text[]");

        modelBuilder.AddQuartz(x => x.UsePostgreSql());

        base.OnModelCreating(modelBuilder);
    }
}
