using System;
using System.Collections.Generic;
using System.Linq;
using AddvalsApi.Dtos;
using AddvalsApi.Model;

namespace AddvalsApi.Data
{
    public class SqlUserRepo : IUserRepo
    {
        private readonly UserContext _context;

        public SqlUserRepo(UserContext context)
        {
            _context = context;
        }

        IEnumerable<UserModel> IUserRepo.GetAllUsers()
        {
            return _context.Users.ToList();
        }

        UserModel IUserRepo.GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(p => p.Id == id);
        }

        public UserModel GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(p => p.Email == email);
        }

         public UserModel GetUserByEmailAndPassword(string email, string password)
        {
            return _context.Users.FirstOrDefault(x => x.Email == email  && x.Password == password );
        } 

        public void CreatUser(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _context.Users.Add(user);
        }
        public void UpdateUser(UserModel user)
        {
            //rien
        }

        public void DeleteUser(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _context.Users.Remove(user);
        }


        public bool SaveChanges()
        {
            return (_context.SaveChanges() >= 0);
        }
    }
}