
namespace MoneyWise.Models
{
    public class LoginVerification
    {
        public bool PasswordChecker(string username, string password)
        {
            return username == "123" && password == "123";
        }       
    }
}