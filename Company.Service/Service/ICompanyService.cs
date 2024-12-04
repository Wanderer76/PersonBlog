using Company.Service.Models;

namespace Company.Service.Service
{
    public interface ICompanyService
    {
        Task<CompanyModel> CreateCompanyAsync(CompanyCreateModel createModel);
        Task<CompanyModel> UpdateCompanyAsync(CompanyUpdateModel createModel);
        Task<IEnumerable<CompanyListModel>> GetCompaniesListAsync(int offset, int limit);
        Task<IEnumerable<CompanyListModel>> GetAvailableCompaniesListAsync(Guid userId, int offset, int limit);
        Task<DetailCompanyModel> GetCompanyByIdAsync(Guid id);
    }
}
