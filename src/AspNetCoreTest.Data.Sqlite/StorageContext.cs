using AspNetCoreTest.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AspNetCoreTest.Data.Models;

namespace AspNetCoreTest.Data.Sqlite
{
    public class StorageContext : DbContext, IStorageContext
    {
        private string connectionString;

        public StorageContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite(this.connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Item>(etb =>
            {
                etb.HasKey(e => e.Id);
                etb.Property(e => e.Id);
                //etb.ForSqliteToTable("Items");
            }
            );
        }
    }
}
