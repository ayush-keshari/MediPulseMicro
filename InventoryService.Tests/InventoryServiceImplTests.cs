using Xunit;
using InventoryService.Models;
using InventoryService.Services;
using InventoryService.DTOs;
using InventoryService.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Tests
{
    public class InventoryServiceTests
    {
        private InventoryDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new InventoryDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task CreateItemAsync_ReturnsError_WhenItemCodeAlreadyExists()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new InventoryServiceImpl(context);

            // Create first item
            var firstRequest = new CreateItemRequest
            {
                ItemCode = "MED-001",
                Name = "Test Item",
                Category = "Pharma",
                Unit = "mg",
                SafetyStock = 10
            };

            await service.CreateItemAsync(firstRequest);

            // Try to create duplicate
            var duplicateRequest = new CreateItemRequest
            {
                ItemCode = "MED-001", // Same code
                Name = "Another Item",
                Category = "Pharma",
                Unit = "ml",
                SafetyStock = 20
            };

            // Act
            var (item, error) = await service.CreateItemAsync(duplicateRequest);

            // Assert
            Assert.Null(item); // Should return null for item
            Assert.NotNull(error); // Should have an error message
            Assert.Contains("already exists", error);
        }

        [Fact]
        public async Task CreateItemAsync_CreatesItemSuccessfully_WhenItemCodeIsUnique()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new InventoryServiceImpl(context);

            var request = new CreateItemRequest
            {
                ItemCode = "MED-001",
                Name = "Test Item",
                Category = "Pharma",
                Unit = "mg",
                StorageRequirement = "Refrigerated",
                SafetyStock = 10
            };

            // Act
            var (item, error) = await service.CreateItemAsync(request);

            // Assert
            Assert.NotNull(item); // Should return the created item
            Assert.Null(error); // Should have no error
            Assert.Equal("MED-001", item.ItemCode);
            Assert.Equal("Test Item", item.Name);
            Assert.Equal("Pharma", item.Category);
            Assert.Equal("mg", item.Unit);
            Assert.Equal("Refrigerated", item.StorageRequirement);
            Assert.Equal(10, item.SafetyStock);

            // Verify item was actually saved in the database
            var savedItem = await context.Items.FirstOrDefaultAsync(i => i.ItemCode == "MED-001");
            Assert.NotNull(savedItem);
            Assert.Equal("Test Item", savedItem.Name);
        }

        [Fact]
        public async Task GetItemByIdAsync_ReturnsNull_WhenItemDoesNotExist()
        {
            await using var context = CreateInMemoryDbContext();
            var service = new InventoryServiceImpl(context);

            Assert.Null(await service.GetItemByIdAsync(999));
        }

        [Fact]
        public async Task UpdateItemAsync_UpdatesOnlyProvidedFields()
        {
            await using var context = CreateInMemoryDbContext();
            var item = new Item
            {
                ItemCode = "MED-001",
                Name = "Original",
                Category = "Pharma",
                Unit = "Box",
                StorageRequirement = "Ambient",
                SafetyStock = 5
            };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = new InventoryServiceImpl(context);

            Assert.True(await service.UpdateItemAsync(item.ItemId, new UpdateItemRequest { Name = "Updated", SafetyStock = 10 }));

            var saved = await context.Items.FindAsync(item.ItemId);
            Assert.Equal("Updated", saved!.Name);
            Assert.Equal(10, saved.SafetyStock);
            Assert.Equal("Pharma", saved.Category);
            Assert.Equal("Box", saved.Unit);
        }

        [Fact]
        public async Task DeleteItemAsync_ReturnsFalseForMissingItem_AndDeletesExistingItem()
        {
            await using var context = CreateInMemoryDbContext();
            var service = new InventoryServiceImpl(context);
            var item = new Item { ItemCode = "MED-001", Name = "Test", Category = "Pharma", Unit = "Box" };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            Assert.False(await service.DeleteItemAsync(999));
            Assert.True(await service.DeleteItemAsync(item.ItemId));
            Assert.Null(await context.Items.FindAsync(item.ItemId));
        }

        [Fact]
        public async Task GetPositionsByItemAsync_OrdersByExpiryAndMapsItemDetails()
        {
            await using var context = CreateInMemoryDbContext();
            var item = new Item { ItemCode = "MED-001", Name = "Test", Category = "Pharma", Unit = "Box" };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.InventoryPositions.AddRange(
                new InventoryPosition { ItemId = item.ItemId, Item = item, LotId = "LATE", ExpiryDate = DateTime.UtcNow.AddDays(30), Quantity = 4, FacilityId = 1, StorageZoneId = 1 },
                new InventoryPosition { ItemId = item.ItemId, Item = item, LotId = "EARLY", ExpiryDate = DateTime.UtcNow.AddDays(5), Quantity = 6, FacilityId = 1, StorageZoneId = 1 });
            await context.SaveChangesAsync();
            var service = new InventoryServiceImpl(context);

            var result = (await service.GetPositionsByItemAsync(item.ItemId)).ToList();

            Assert.Equal(new[] { "EARLY", "LATE" }, result.Select(p => p.LotId));
            Assert.Equal("Test", result[0].ItemName);
            Assert.Equal("MED-001", result[0].ItemCode);
        }

        [Fact]
        public async Task GetFacilityStockAsync_ExcludesEmptyPositionsAndAggregatesByItem()
        {
            await using var context = CreateInMemoryDbContext();
            context.InventoryPositions.AddRange(
                new InventoryPosition { ItemId = 1, FacilityId = 7, Quantity = 4 },
                new InventoryPosition { ItemId = 1, FacilityId = 7, Quantity = 6 },
                new InventoryPosition { ItemId = 2, FacilityId = 7, Quantity = 0 },
                new InventoryPosition { ItemId = 3, FacilityId = 8, Quantity = 9 });
            await context.SaveChangesAsync();
            var service = new InventoryServiceImpl(context);

            var result = (await service.GetFacilityStockAsync(7)).ToList();

            var stock = Assert.Single(result);
            Assert.Equal(1, stock.ItemId);
            Assert.Equal(10, stock.AvailableQty);
        }

        [Fact]
        public async Task GetAllItemsAsync_ReturnsItemsWithTotalStock()
        {
            await using var context = CreateInMemoryDbContext();
            var item = new Item { ItemCode = "MED-001", Name = "Test", Category = "Pharma", Unit = "Box" };
            item.Positions.Add(new InventoryPosition { Quantity = 7, FacilityId = 1, StorageZoneId = 1 });
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var result = (await new InventoryServiceImpl(context).GetAllItemsAsync()).ToList();

            var response = Assert.Single(result);
            Assert.Equal("MED-001", response.ItemCode);
            Assert.Equal(7, response.TotalStock);
        }

        [Fact]
        public async Task PositionQueries_ReturnDistinctPositiveLocations()
        {
            await using var context = CreateInMemoryDbContext();
            context.Items.Add(new Item { ItemCode = "MED-002", Name = "Second", Category = "Pharma", Unit = "Box" });
            context.InventoryPositions.AddRange(
                new InventoryPosition { ItemId = 1, FacilityId = 2, StorageZoneId = 1, Quantity = 3 },
                new InventoryPosition { ItemId = 2, FacilityId = 2, StorageZoneId = 2, Quantity = 4 },
                new InventoryPosition { ItemId = 1, FacilityId = 3, StorageZoneId = 3, Quantity = 0 });
            await context.SaveChangesAsync();

            var service = new InventoryServiceImpl(context);

            Assert.Equal(new[] { 2 }, (await service.GetFacilityIdsByItemAsync(1)).ToArray());
            Assert.Equal(new[] { 1, 2 }, (await service.GetItemIdsByFacilityAsync(2)).ToArray());
        }

        [Fact]
        public async Task PositionCrud_ReturnsExpectedResults()
        {
            await using var context = CreateInMemoryDbContext();
            var service = new InventoryServiceImpl(context);
            var item = new Item { ItemCode = "MED-001", Name = "Test", Category = "Pharma", Unit = "Box" };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var request = new CreatePositionRequest
            {
                ItemId = item.ItemId, LotId = "LOT-1", ExpiryDate = DateTime.UtcNow.AddDays(30),
                Quantity = 10, FacilityId = 2, StorageZoneId = 3, SafetyStock = 2
            };

            Assert.True(await service.CreatePositionAsync(request));
            var position = await context.InventoryPositions.SingleAsync();
            Assert.Single(await service.GetAllPositionsAsync());
            Assert.True(await service.UpdatePositionAsync(position.PositionId, new UpdatePositionRequest
            {
                Quantity = 4, FacilityId = 4, StorageZoneId = 5, SafetyStock = 1,
                ExpiryDate = DateTime.UtcNow.AddDays(60)
            }));
            Assert.Equal(4, (await context.InventoryPositions.FindAsync(position.PositionId))!.Quantity);
            Assert.True(await service.DeletePositionAsync(position.PositionId));
            Assert.False(await service.DeletePositionAsync(position.PositionId));
        }
    }
}
