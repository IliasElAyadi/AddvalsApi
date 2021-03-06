using System.Collections.Generic;
using AddvalsApi.Dtos;
using AddvalsApi.Model;

namespace AddvalsApi.Data
{
    public interface IUserRepo
    {
        bool SaveChanges();
        IEnumerable<UserModel> GetAllUsers();
        UserModel GetUserById(int id);
        UserModel GetUserByEmail(string email);
        UserModel GetUserByEmailAndPassword(string email, string password);
        void CreatUser(UserModel user);
        void UpdateUser(UserModel user);
        void DeleteUser(UserModel user);

    }
}