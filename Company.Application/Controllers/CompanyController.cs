using Authentication.Service.Service;
using Company.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace Company.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet("/availableCompanies")]
        [Authorize]
        public async Task<IActionResult> GetAvailableCompanies()
        {
            var userId = HttpContext.GetUserFromContext();
            var result = await _companyService.GetAvailableCompaniesListAsync(userId, 0, 100);
            return Ok(result);
        }

        [HttpGet("/company/{id:guid}")]
        public async Task<IActionResult> GetDetailCompanyInfo(Guid id)
        {
            var result = await _companyService.GetCompanyByIdAsync(id);
            return Ok(result);
        }

    }
}
