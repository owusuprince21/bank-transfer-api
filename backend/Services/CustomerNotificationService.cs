using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Hubs;
using ApiDemo.Models;
using Microsoft.AspNetCore.SignalR;

namespace ApiDemo.Services;

public class CustomerNotificationService
{
    private readonly BankingDbContext _dbContext;
    private readonly IHubContext<NotificationsHub> _hubContext;

    public CustomerNotificationService(BankingDbContext dbContext, IHubContext<NotificationsHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<CustomerNotificationResponse> NotifyAsync(
        Guid customerId,
        string type,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var notification = new CustomerNotification
        {
            CustomerId = customerId,
            Type = type,
            Title = title,
            Message = message
        };

        _dbContext.CustomerNotifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(notification);
        await _hubContext
            .Clients
            .Group(NotificationsHub.GetCustomerGroup(customerId))
            .SendAsync("notificationReceived", response, cancellationToken);

        return response;
    }

    public static CustomerNotificationResponse ToResponse(CustomerNotification notification)
    {
        return new CustomerNotificationResponse(
            notification.Id,
            notification.CustomerId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAtUtc);
    }
}
