using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.Service.Models
{
    public class DetailCompanyModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<DepartmentViewModel> Departments { get; set; }
    }
}
