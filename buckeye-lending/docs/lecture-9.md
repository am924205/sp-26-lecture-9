# Lecture 9 - Concepts

## Implementing the Queueing Entities

### Step 1: ReviewQueue Model

Create `Models/ReviewQueue.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Buckeye.Lending.Api.Models;

public class ReviewQueue
{
    public int Id { get; set; }

    [Required]
    public string OfficerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property — one queue has many items
    public ICollection<ReviewItem> Items { get; set; } = new List<ReviewItem>();
}
```

**Concept:** Simple. An Id, an OfficerId — required, because every queue must belong to someone — timestamps, and a navigation property for the items collection. The `= new List<>()` default prevents null reference exceptions when you access Items on a new queue.

### Step 2: ReviewItem Model

Create `Models/ReviewItem.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Buckeye.Lending.Api.Models;

public class ReviewItem
{
    public int Id { get; set; }

    // Foreign key to the queue
    public int QueueId { get; set; }
    public ReviewQueue Queue { get; set; } = null!;

    // Foreign key to the loan application
    public int LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;

    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    public string? Notes { get; set; }
}
```

**Concept:** ReviewItem has two foreign keys: QueueId links it to the queue, LoanApplicationId links it to the loan application being reviewed. Both have navigation properties so we can use `.Include()` in queries. Priority defaults to 3 — middle of the range. Notes are nullable because they're optional.

The `= null!` on the navigation properties tells the compiler 'I know this looks null, but EF Core will populate it when I use Include.' This is standard EF Core convention.

### Step 3: Update DbContext

Open `Data/LendingContext.cs` and add:

```csharp
public DbSet<ReviewQueue> ReviewQueues { get; set; }
public DbSet<ReviewItem> ReviewItems { get; set; }
```

### Step 4: Run Migration

First, we need to install the EF Core tools if you haven't already:

```bash
dotnet tool install --global dotnet-ef
```

Then, create and apply the migration:

```bash
dotnet ef migrations add AddReviewQueue
dotnet ef database update
```

**Concept:** This will create the necessary tables in the database for our new models. The migration will include the creation of the `ReviewQueues` and `ReviewItems` tables, along with the appropriate foreign key constraints.

Migrations are a powerful feature of EF Core that allow us to evolve our database schema over time as our application requirements change. By creating a migration, we can ensure that our database stays in sync with our application's data model.

---

## Controller Skeleton

Create `Dtos/ReviewQueueRequests.cs`:

```csharp
namespace Buckeye.Lending.Api.Dtos;

public class AddToQueueRequest
{
    public int LoanApplicationId { get; set; }
    public int Priority { get; set; } = 3;
}

public class UpdateItemRequest
{
    public int Priority { get; set; }
    public string? Notes { get; set; }
}
```

Create `Controllers/ReviewQueueController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Buckeye.Lending.Api.Data;
using Buckeye.Lending.Api.Models;
using Buckeye.Lending.Api.Dtos;

namespace Buckeye.Lending.Api.Controllers;

[ApiController]
[Route("api/review-queue")]
public class ReviewQueueController : ControllerBase
{
    private readonly LendingContext _context;
    private const string CurrentOfficerId = "default-officer";

    public ReviewQueueController(LendingContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ReviewQueue>> GetQueue()
    {
        // TODO: implement
        throw new NotImplementedException();
    }

    [HttpPost]
    public async Task<ActionResult<ReviewItem>> AddToQueue(AddToQueueRequest request)
    {
        // TODO: implement
        throw new NotImplementedException();
    }

    [HttpPut("{itemId}")]
    public async Task<ActionResult<ReviewItem>> UpdateItem(int itemId, UpdateItemRequest request)
    {
        // TODO: implement
        throw new NotImplementedException();
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        // TODO: implement
        throw new NotImplementedException();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearQueue()
    {
        // TODO: implement
        throw new NotImplementedException();
    }
}
```

**Concept:** Shape first, logic second. Five endpoints. The constant at the top — that's our identity strategy for now. Constructor injection of the DbContext — same as every controller you've built.

Three things to notice.

1. `ActionResult<ReviewQueue>` on GetQueue — we return data, so we use the generic version. `IActionResult` on the deletes — no data to return, just a status code.
2. The route attribute is `api/review-queue`, not `api/[controller]` — because the controller name is `ReviewQueueController` but we want a hyphenated route.
3. Two DELETE routes — `{itemId}` and `clear`. ASP.NET can distinguish them because the route templates are different.

---

## Implementing GetQueue

Replace the GetQueue TODO:

```csharp
[HttpGet]
public async Task<ActionResult<ReviewQueue>> GetQueue()
{
    var queue = await _context.ReviewQueues
        .Include(q => q.Items)
        .ThenInclude(i => i.LoanApplication)
        .FirstOrDefaultAsync(q => q.OfficerId == CurrentOfficerId);

    if (queue == null)
    {
        return NotFound();
    }

    return Ok(queue);
}
```

**Concept:** Three lines of real logic. Find the queue for this officer, including items and their loan applications. If it doesn't exist, return 404. If it does, return 200 with the full queue.

This is a design decision we made: if the officer hasn't added anything yet, there is no queue. The frontend gets a 404 and displays an empty state — 'no applications in your review queue.' The alternative would be to auto-create an empty queue and return 200, but then you have empty queue records sitting in your database for every officer who's never used the feature. 404 is cleaner.

For M4: same decision. If the user hasn't added anything to their cart, return 404. Your React code handles it as 'your cart is empty.'

---

## Implementing AddToQueue (Upsert Pattern)

Replace the AddToQueue TODO:

```csharp
[HttpPost]
public async Task<ActionResult<ReviewItem>> AddToQueue(AddToQueueRequest request)
{
    // 1. Verify the loan application exists
    var loanApp = await _context.LoanApplications.FindAsync(request.LoanApplicationId);
    if (loanApp == null)
    {
        return BadRequest($"Loan application {request.LoanApplicationId} not found.");
    }

    // 2. Find or create the queue for this officer
    var queue = await _context.ReviewQueues
        .Include(q => q.Items)
        .FirstOrDefaultAsync(q => q.OfficerId == CurrentOfficerId);

    if (queue == null)
    {
        queue = new ReviewQueue { OfficerId = CurrentOfficerId };
        _context.ReviewQueues.Add(queue);
    }

    // 3. Check if this loan application is already in the queue (UPSERT)
    var existingItem = queue.Items
        .FirstOrDefault(i => i.LoanApplicationId == request.LoanApplicationId);

    if (existingItem != null)
    {
        // Update — loan already in queue, just update priority
        existingItem.Priority = request.Priority;
        queue.UpdatedAt = DateTime.UtcNow;
    }
    else
    {
        // Insert — new item
        var newItem = new ReviewItem
        {
            LoanApplicationId = request.LoanApplicationId,
            Priority = request.Priority
        };
        queue.Items.Add(newItem);
        queue.UpdatedAt = DateTime.UtcNow;
    }

    // 4. Save everything in one transaction
    await _context.SaveChangesAsync();

    // 5. Reload with navigation properties for the response
    var savedItem = await _context.ReviewItems
        .Include(i => i.LoanApplication)
        .FirstAsync(i => i.QueueId == queue.Id
            && i.LoanApplicationId == request.LoanApplicationId);

    return CreatedAtAction(nameof(GetQueue), savedItem);
}
```

**Concept:** This is the most complex endpoint, so let's break it down.

**Step 1:** Verify the loan application exists. If someone sends a request with a bad ID, we catch it early with a 400 Bad Request. Don't skip this — without it, you'd get a foreign key violation from the database, which is a 500 error. Much worse.

**Step 2:** Find or create the queue. This is the first new pattern. With products, you never had to 'find or create a container' — you just inserted. Here, the queue might not exist yet. If it doesn't, we create one. If it does, we use the existing one.

**Step 3:** The upsert. Check if this loan application is already in the queue. If yes — update the priority. If no — create a new ReviewItem and add it to the queue. Either way, update the timestamp.

This is the pattern you'll use for M4: check if the product is already in the cart. If yes, increment the quantity. If no, create a new CartItem.

**Step 4:** One SaveChanges call. Whether we created a queue, updated an existing item, or added a new one — it all saves in one database transaction. Either everything succeeds or nothing does. That's data consistency.

**Step 5:** Reload the saved item with its navigation properties so the response includes the loan application details. Return 201 Created."

**Summary:**

1. First POST — 201 Created. GET — there's our queue with one item.
2. POST with the same loan ID but different priority — 201 again, but look at the GET: still one item, priority updated.
3. That's the upsert working. No duplicates.

---
