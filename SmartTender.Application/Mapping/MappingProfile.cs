using AutoMapper;
using SmartTender.Domain;
using SmartTender.Application.DTOs;

namespace SmartTender.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Tender, TenderDto>()
                .ForMember(dest => dest.CategoryCodes, opt => opt.MapFrom(src => src.TenderCategories != null ? src.TenderCategories.Select(tc => tc.Category.Code).ToList() : new List<string>()))
                .ForMember(dest => dest.BomLines, opt => opt.MapFrom(src => src.BomLines))
                .ForMember(dest => dest.ContactPersons, opt => opt.MapFrom(src => src.ContactPersons))
                .ForMember(dest => dest.Announcements, opt => opt.MapFrom(src => src.Announcements));

            CreateMap<BomLine, BomLineDto>();
            CreateMap<ContactPerson, ContactPersonDto>();
            CreateMap<Announcement, AnnouncementDto>();
            CreateMap<Category, CategoryDto>();
        }
    }
} 