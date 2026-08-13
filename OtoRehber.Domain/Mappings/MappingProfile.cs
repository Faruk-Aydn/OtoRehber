using AutoMapper;
using OtoRehber.Domain.DTOs;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity'den DTO'ya ve tam tersine (ReverseMap) mapleme
            CreateMap<Car, CarCreateDto>().ReverseMap();
            CreateMap<Car, CarListDto>().ReverseMap();
            CreateMap<Car, CarDetailDto>().ReverseMap();
        }
    }
}
