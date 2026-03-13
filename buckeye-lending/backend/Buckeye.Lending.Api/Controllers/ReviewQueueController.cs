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

    [HttpPut("{itemId}")]
    public async Task<ActionResult<ReviewItem>> UpdateItem(int itemId, UpdateItemRequest request)
    {
        var item = await _context.ReviewItems
            .Include(i => i.LoanApplication)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
        {
            return NotFound();
        }

        // Verify the item belongs to the current officer's queue
        var queue = await _context.ReviewQueues.FindAsync(item.QueueId);
        if (queue == null || queue.OfficerId != CurrentOfficerId)
        {
            return NotFound();
        }

        item.Priority = request.Priority;
        item.Notes = request.Notes;
        queue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var item = await _context.ReviewItems.FindAsync(itemId);

        if (item == null)
        {
            return NotFound();
        }

        // Verify the item belongs to the current officer's queue
        var queue = await _context.ReviewQueues.FindAsync(item.QueueId);
        if (queue == null || queue.OfficerId != CurrentOfficerId)
        {
            return NotFound();
        }

        _context.ReviewItems.Remove(item);
        queue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearQueue()
    {
        var queue = await _context.ReviewQueues
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.OfficerId == CurrentOfficerId);

        if (queue == null)
        {
            return NotFound();
        }

        _context.ReviewItems.RemoveRange(queue.Items);
        queue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
