using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using ApiDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/financial-controls")]
[Authorize]
public class FinancialControlsController : ControllerBase
{
    private readonly BankingDbContext _dbContext;
    private readonly CustomerNotificationService _notificationService;

    public FinancialControlsController(BankingDbContext dbContext, CustomerNotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<SpendingControlResponse?>> GetMine()
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var control = await _dbContext.SpendingControls
            .AsNoTracking()
            .Where(existingControl => existingControl.CustomerId == customerId.Value)
            .OrderByDescending(existingControl => existingControl.UpdatedAtUtc)
            .FirstOrDefaultAsync();

        return Ok(control is null ? null : ToResponse(control));
    }

    [HttpPut("me")]
    public async Task<ActionResult<SpendingControlResponse>> UpdateMine(SpendingControlRequest request)
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var control = await GetOrCreateControl(customerId.Value);
        control.MonthlySpendLimit = request.MonthlySpendLimit;
        control.SingleTransactionLimit = request.SingleTransactionLimit;
        control.SavingsTarget = request.SavingsTarget;
        control.BlockTransfersWhenLimitReached = request.BlockTransfersWhenLimitReached;
        control.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _notificationService.NotifyAsync(
            customerId.Value,
            "FinancialControlsUpdated",
            "Money controls updated",
            $"Monthly limit: {request.MonthlySpendLimit:N2}. Single transaction limit: {request.SingleTransactionLimit:N2}.");

        return Ok(ToResponse(control));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMine()
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var control = await _dbContext.SpendingControls
            .Where(existingControl => existingControl.CustomerId == customerId.Value)
            .OrderByDescending(existingControl => existingControl.UpdatedAtUtc)
            .FirstOrDefaultAsync();

        if (control is null)
        {
            return NoContent();
        }

        _dbContext.SpendingControls.Remove(control);
        await _dbContext.SaveChangesAsync();
        await _notificationService.NotifyAsync(
            customerId.Value,
            "FinancialControlsDeleted",
            "Money controls deleted",
            "Your saved financial management controls were removed.");

        return NoContent();
    }

    private async Task<SpendingControl> GetOrCreateControl(Guid customerId)
    {
        var controls = await _dbContext.SpendingControls
            .Where(existingControl => existingControl.CustomerId == customerId)
            .OrderByDescending(existingControl => existingControl.UpdatedAtUtc)
            .ToListAsync();

        var control = controls.FirstOrDefault();

        if (control is not null)
        {
            if (controls.Count > 1)
            {
                _dbContext.SpendingControls.RemoveRange(controls.Skip(1));
            }

            return control;
        }

        control = new SpendingControl
        {
            CustomerId = customerId,
            MonthlySpendLimit = 0,
            SingleTransactionLimit = 0,
            SavingsTarget = 0,
            BlockTransfersWhenLimitReached = false
        };

        _dbContext.SpendingControls.Add(control);
        return control;
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
    }

    private static SpendingControlResponse ToResponse(SpendingControl control)
    {
        return new SpendingControlResponse(
            control.Id,
            control.CustomerId,
            control.MonthlySpendLimit,
            control.SingleTransactionLimit,
            control.SavingsTarget,
            control.BlockTransfersWhenLimitReached,
            control.UpdatedAtUtc);
    }
}
