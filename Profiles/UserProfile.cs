using AddvalsApi.Dtos;
using AddvalsApi.Model;
using AutoMapper;

namespace AddvalsApi.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            //De la source à la direction
            CreateMap<UserModel,UserReadDto>();
            CreateMap<UserCreateDto,UserModel>();
            CreateMap<UserUpdateDto,UserModel>();
            CreateMap<UserModel,UserUpdateDto>();
        }
    }
}