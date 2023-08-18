﻿using Todo.Infra.Context;
using Todo.Domain.Models;
using Todo.Infra.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Todo.Infra.Repository;

public class UserRepository : Repository<User>, IUserRepository
{
    private readonly TodoDbContext _context;

    public UserRepository(TodoDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmail(string email)
    {
        var user = await _context.Users
            .Where(x => x.Email.ToLower() == email.ToLower())
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return user;
    }
}