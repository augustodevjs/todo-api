﻿using AutoMapper;
using Todo.Domain.Models;
using System.Security.Claims;
using Todo.Domain.Validators;
using Microsoft.AspNetCore.Http;
using Todo.Infra.Contracts;
using Todo.Services.Contracts;
using Todo.Services.DTO.AssignmentList;

namespace Todo.Services.Services;

public class AssignmentListService : BaseService, IAssignmentListService
{
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAssignmentListRepository _assignmentListRepository;
    
    public AssignmentListService(
        IMapper mapper, 
        INotificator notificator, 
        IHttpContextAccessor httpContextAccessor,
        IAssignmentListRepository assignmentListRepository 
        ): base(notificator)
    {
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _assignmentListRepository = assignmentListRepository;
    }

    public async Task<AssignmentListDto?> GetById(string? id)
    {
        var getUser = await _assignmentListRepository.GetById(id);
        
        if (getUser == null)
        {
            Notify("O ID fornecido é inválido. Não foi possível encontrar o usuário correspondente.");
            return null;
        };

        return _mapper.Map<AssignmentListDto>(getUser);
    }
        
    public async Task<AssignmentListDto?> Create(AddAssignmentListDto addAssignmentListDto)
    {
        var assignmentList = _mapper.Map<AssignmentList>(addAssignmentListDto);
        assignmentList.UserId = GetUserId();
        
        if (!ExecuteValidation(new AssignmentListValidator(), assignmentList)) return null;
        
        await _assignmentListRepository.Create(assignmentList);
        
        return _mapper.Map<AssignmentListDto>(assignmentList);
    }

    public async  Task<AssignmentListDto?> Update(string id, UpdateAssignmentListDto updateAssignmentListDto)
    {
        if (id != updateAssignmentListDto.Id)
        {
            Notify("O id informado é inválido");
            return null;
        }

        var getAssignmentList = await _assignmentListRepository.GetById(id);

        if (getAssignmentList == null)
        {
            Notify("O ID fornecido é inválido. Não foi possível encontrar o usuário correspondente.");
            return null;
        }

        _mapper.Map(updateAssignmentListDto, getAssignmentList);

        if (!ExecuteValidation(new AssignmentListValidator(), getAssignmentList)) return null;
        
        await _assignmentListRepository.Update(getAssignmentList);
        
        return _mapper.Map<AssignmentListDto>(updateAssignmentListDto);
    }

    private string GetUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId == null ? string.Empty : userId;
    }
}