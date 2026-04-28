using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Sku> Skus => Set<Sku>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Characteristic> Characteristics => Set<Characteristic>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CategoryFilter> CategoryFilters => Set<CategoryFilter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("catalog");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        }
    }

}
