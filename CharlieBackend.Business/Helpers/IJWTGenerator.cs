using CharlieBackend.Core.DTO.Account;

namespace CharlieBackend.Business.Helpers
{
    public interface IJWTGenerator
    {
        string GenerateEncodedJWT(AccountDto account);
    }
}
