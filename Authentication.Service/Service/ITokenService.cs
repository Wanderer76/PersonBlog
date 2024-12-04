using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Service.Service
{
    public interface ITokenService
    {
        Task ClearUserToken(string token);
        bool Validate(string token);
        AuthResponse GenerateToken(AppUser user);
        TokenModel GetTokenRepresentaion(string token);
    }
}
