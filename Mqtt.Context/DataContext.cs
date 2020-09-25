using System.Collections.Generic;
using Mqtt.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Mqtt.Context
{
    public class DataContext : DbContext
    {
        public DbSet<MqttUser> MqttUsers { get; set; }
        public DbSet<MqttMessage> MqttMessages { get; set; }
        public DbSet<Audit> Audit { get; set; }
        public DbSet<Log> Log { get; set; }
        public DbSet<Connection> Connection { get; set; }
        public DbSet<Payload> Payload { get; set; }
        public DbSet<Platoon> Platoon { get; set; }
        public DbSet<Subscribe> Subscribe { get; set; }

        public DataContext(DbContextOptions<DataContext> options) :base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MqttUser>().HasKey(user => user.Username);
            modelBuilder.Entity<MqttMessage>().HasIndex(m => m.Created);

            modelBuilder.Entity<Log>();
            modelBuilder.Entity<Payload>();
            modelBuilder.Entity<Connection>();
            modelBuilder.Entity<Platoon>();
            modelBuilder.Entity<Subscribe>();
            modelBuilder.Entity<Audit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ClientId).HasColumnName("ClientId");
                entity.Property(e => e.Topic).HasColumnName("Topic");
            });
        }
    }
}