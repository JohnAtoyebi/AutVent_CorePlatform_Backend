using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/bank-accounts")]
[Authorize]
public class BankAccountController(IBankAccountService bankAccountService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IList<BankAccountResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await bankAccountService.GetAllAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, [FromBody] CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{id:long}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetDefault(long id, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.SetDefaultAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
