using AspNetCoreTest.Data.Abstractions;
using AspNetCoreTest.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Sqlite
{
    public class ItemRepository : IItemRepository
    {
        private StorageContext storageContext;
        private DbSet<Item> dbSet;

        public void SetStorageContext(IStorageContext storageContext)
        {
            this.storageContext = storageContext as StorageContext;
            this.dbSet = this.storageContext.Set<Item>();
        }

        public IEnumerable<Item> All()
        {
            return this.dbSet.OrderBy(i => i.Name);
        }
    }
}
