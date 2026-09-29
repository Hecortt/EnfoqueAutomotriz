using AutoMapper;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.Auth.DTOs;

namespace EnfoqueAutomotriz.Api.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Usuario, UsuarioDto>();
    }
}
