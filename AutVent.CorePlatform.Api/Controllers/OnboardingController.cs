using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        this.onboardingService = onboardingService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var response = await onboardingService.RegisterAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var response = await onboardingService.VerifyOtpAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(ApiResponse<ResendOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResendOtpResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ResendOtpResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<ResendOtpResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken cancellationToken)
    {
        var response = await onboardingService.ResendOtpAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
