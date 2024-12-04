using Company.Domain.Entities;
using Company.Service.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.Service.Service.Impl
{
    internal class DefaultCompanyService : ICompanyService
    {
        private readonly IReadWriteRepository<ICompanyEntity> _context;

        public DefaultCompanyService(IReadWriteRepository<ICompanyEntity> context)
        {
            _context = context;
        }

        public Task<CompanyModel> CreateCompanyAsync(CompanyCreateModel createModel)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CompanyListModel>> GetAvailableCompaniesListAsync(Guid userId, int offset, int limit)
        {
            var companies = await _context.Get<Organization>()
                .Where(x => x.DirectorId == userId)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            return companies.Select(c => new CompanyListModel
            {
                Id = c.Id,
                Name = c.Name,
                Address = c.Address,
            }).ToList();

        }

        public Task<IEnumerable<CompanyListModel>> GetCompaniesListAsync(int offset, int limit)
        {
            throw new NotImplementedException();
        }

        public async Task<DetailCompanyModel> GetCompanyByIdAsync(Guid id)
        {
            var company = await _context.Get<Organization>()
                .Where(x => x.Id == id)
                .Include(x => x.Departments)
                .FirstAsync();

            var result = new DetailCompanyModel
            {
                Id = company.Id,
                Name = company.Name,
                Departments = company.Departments.Select(department => new DepartmentViewModel(department.Id, department.Name, department.Description, department.CreatedAt, department.CloseAt, "заглушка"))
            };
            return result;
        }

        public Task<CompanyModel> UpdateCompanyAsync(CompanyUpdateModel createModel)
        {
            throw new NotImplementedException();
        }
    }
}
