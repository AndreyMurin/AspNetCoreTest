using AspNetCoreTest.Data.Abstractions;
using AspNetCoreTest.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Mock
{
    public class ItemRepository : IItemRepository
    {
        public readonly IList<Item> items;

        public ItemRepository()
        {
            this.items = new List<Item>();
            this.items.Add(new Item() { Id = 1, Name = "Mock item 1" });
            this.items.Add(new Item() { Id = 2, Name = "Mock item 2" });
            this.items.Add(new Item() { Id = 3, Name = "Mock item 3" });
        }

        public void SetStorageContext(IStorageContext storageContext)
        {
            // Do nothing
        }

        public IEnumerable<Item> All()
        {
            return this.items.OrderBy(i => i.Name);
        }
    }
}
