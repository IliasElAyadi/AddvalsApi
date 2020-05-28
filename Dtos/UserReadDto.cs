
namespace AddvalsApi.Dtos
{
    public class UserReadDto
    {
        public int Id { get; set; }

        public string Login { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string tokenApi { get; set; }
        public string TokenSkytap { get; set; }
        public string idSkytap { get; set; }



    }
}