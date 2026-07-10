using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class OnboardingProgressService(IUnitOfWork unitOfWork) : IOnboardingProgressService
{
    public async Task<ApiResponse<OnboardingProgressResponse>> GetProgressAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<OnboardingProgressResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        var accountCreated = true;
        var emailVerified = user.IsActive;

        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var businessCreated = business is not null;

        var storeCreated = false;
        var productAdded = false;

        if (businessCreated)
        {
            storeCreated = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.BusinessId == business!.Id, cancellationToken);

            if (storeCreated)
            {
                var storeIds = await unitOfWork.Query<Store>()
                    .Where(x => x.BusinessId == business!.Id)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                productAdded = await unitOfWork.Query<Product>()
                    .AnyAsync(x => storeIds.Contains(x.StoreId), cancellationToken);
            }
        }

        var steps = new[] { accountCreated, emailVerified, businessCreated, storeCreated, productAdded };
        var completed = steps.Count(s => s);
        const int total = 5;
        var percent = (int)Math.Round(completed / (double)total * 100);

        var response = new OnboardingProgressResponse
        {
            AccountCreated = accountCreated,
            EmailVerified = emailVerified,
            BusinessCreated = businessCreated,
            StoreCreated = storeCreated,
            ProductAdded = productAdded,
            CompletedSteps = completed,
            TotalSteps = total,
            ProgressPercent = percent
        };

        return ApiResponse<OnboardingProgressResponse>.Ok(response);
    }
}
