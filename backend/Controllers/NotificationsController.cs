using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly BankingDbContext _dbContext;

    public NotificationsController(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<CustomerNotificationResponse>>> GetMine()
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var notifications = await _dbContext.CustomerNotifications
            .AsNoTracking()
            .Where(notification => notification.CustomerId == customerId.Value)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(50)
            .Select(notification => CustomerNotificationService.ToResponse(notification))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<CustomerNotificationResponse>> MarkAsRead(Guid id)
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var notification = await _dbContext.CustomerNotifications
            .FirstOrDefaultAsync(existingNotification => existingNotification.Id == id
                && existingNotification.CustomerId == customerId.Value);

        if (notification is null)
        {
            return NotFound(new { message = "Notification was not found." });
        }

        notification.IsRead = true;
        await _dbContext.SaveChangesAsync();

        return Ok(CustomerNotificationService.ToResponse(notification));
    }

    [HttpPost("me/read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        await _dbContext.CustomerNotifications
            .Where(notification => notification.CustomerId == customerId.Value && !notification.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.IsRead, true));

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var notification = await _dbContext.CustomerNotifications
            .FirstOrDefaultAsync(existingNotification => existingNotification.Id == id
                && existingNotification.CustomerId == customerId.Value);

        if (notification is null)
        {
            return NotFound(new { message = "Notification was not found." });
        }

        _dbContext.CustomerNotifications.Remove(notification);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
    }
}
