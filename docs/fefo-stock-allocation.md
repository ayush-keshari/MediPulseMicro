# FEFO Stock Allocation

Transfer completion uses a first-expiry-first-out (FEFO) allocation transform for stock movement.

Given a requested item quantity and the positive source lots at the origin facility, the allocator:

1. Orders lots by `ExpiryDate`, then by `PositionId` as a deterministic tie-breaker.
2. Allocates the minimum of each lot's available quantity and the remaining transfer quantity.
3. Produces an explicit allocation plan without mutating persistence entities.
4. Applies the plan to source inventory and uses the earliest selected lot as the destination metadata template.

The algorithm guarantees that no lot contributes more than its available quantity and that earlier-expiring stock is consumed before later-expiring stock. Partial multi-lot transfers are supported, and the plan reports any unallocated quantity for callers that need to enforce an availability policy.

Implementation: `LogisticsService/Services/FefoStockAllocationService.cs`  
Integration point: `LogisticsService/Services/LogisticsServiceImpl.cs`  
Coverage: `LogisticsService.Tests/FefoStockAllocationServiceTests.cs` and transfer-completion tests.
