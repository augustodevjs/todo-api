﻿using AutoMapper;
using System.Text;
using Todo.Services.DTO;
using Todo.Domain.Models;
using Todo.Core.Interfaces;
using Todo.Infra.Interfaces;
using Todo.Domain.Validators;
using System.Security.Claims;
using Todo.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace Todo.Services.Services;

public class AuthService : BaseService, IAuthService
{
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        IMapper mapper,
        INotificator notificator,
        IConfiguration configuration,
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher
    ) : base(notificator)
    {
        _mapper = mapper;
        _configuration = configuration;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterDTO?> Create(UserDTO userDto)
    {
        var userMapper = _mapper.Map<User>(userDto);

        if (!ExecutarValidacao(new UserValidator(), userMapper)) return null;

        var getUser = await _userRepository.GetByEmail(userDto.Email);

        if (getUser != null)
        {
            Notificar("Já existe um usuário cadastrado com o email informado.");
            return null;
        }

        userMapper.Password = _passwordHasher.HashPassword(userMapper, userDto.Password);

        await _userRepository.Create(userMapper);

        return _mapper.Map<RegisterDTO>(userDto);
    }

    public async Task<TokenDTO?> Login(LoginDTO loginDto)
    {
        var userMapper = _mapper.Map<User>(loginDto);

        if (!ExecutarValidacao(new LoginValidator(), userMapper)) return null;

        var user = await _userRepository.GetByEmail(loginDto.Email);

        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password) !=
            PasswordVerificationResult.Success)
        {
            Notificar("Usuário ou senha estão incorretos.");
            return null;
        }

        return GenerateToken(user);
    }

    private TokenDTO GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(_configuration["AppSettings:Secret"] ?? string.Empty);

        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.Role, "User"),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            }),
            SigningCredentials =
                new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
            Expires =
                DateTime.UtcNow.AddHours(int.Parse(_configuration["AppSettings:ExpirationHours"] ?? string.Empty)),
            Issuer = _configuration["AppSettings:Issuer"],
            Audience = _configuration["AppSettings:ValidOn"]
        });

        var encodedToken = tokenHandler.WriteToken(token);

        return new TokenDTO
        {
            accessToken = encodedToken,
            expiresIn = TimeSpan.FromHours(int.Parse(_configuration["AppSettings:ExpirationHours"] ?? string.Empty))
                .TotalSeconds
        };
    }
}