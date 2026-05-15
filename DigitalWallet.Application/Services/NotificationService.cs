using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.Notification;
using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;

namespace DigitalWallet.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICachingService _cache;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICachingService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ServiceResult<IEnumerable<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var cacheKey = CacheKeys.UserNotifications(userId, pageNumber);

                var notifications = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var data = await _unitOfWork.Notifications.GetByUserIdAsync(
                            userId, pageNumber, pageSize);

                        return _mapper.Map<IEnumerable<NotificationDto>>(data);
                    },
                    TimeSpan.FromMinutes(10)
                );

                return ServiceResult<IEnumerable<NotificationDto>>.Success(notifications);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<NotificationDto>>.Failure(
                    $"Error retrieving notifications: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> MarkAsReadAsync(Guid notificationId)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
                if (notification == null)
                    return ServiceResult<bool>.Failure("Notification not found");

                await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
                await _unitOfWork.SaveChangesAsync();

                await _cache.RemoveAsync(CacheKeys.UnreadNotificationCount(notification.UserId));
                await _cache.RemoveByPatternAsync($"notifications:user:{notification.UserId}*");

                return ServiceResult<bool>.Success(true, "Notification marked as read");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure(
                    $"Error marking notification as read: {ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var cacheKey = CacheKeys.UnreadNotificationCount(userId);

                var count = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        return await _unitOfWork.Notifications.GetUnreadCountAsync(userId);
                    },
                    TimeSpan.FromMinutes(5) 
                );

                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure(
                    $"Error getting unread count: {ex.Message}");
            }
        }
    }
}
