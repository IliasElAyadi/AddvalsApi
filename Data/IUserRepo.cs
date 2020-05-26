using System.Collections.Generic;
using AddvalsApi.Model;

namespace AddvalsApi.Data
{
    public interface IUserRepo
    {
        bool SaveChanges();
        IEnumerable<UserModel> GetAllUsers();
        UserModel GetUserById(int id);
        UserModel GetUserByLogin(string login);
        UserModel GetUserByLoginAndPassword(string login, string password);
        void CreatUser(UserModel user);
        void UpdateUser(UserModel user);
        void DeleteUser(UserModel user);

    }
}