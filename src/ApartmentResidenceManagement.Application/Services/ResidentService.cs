using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class ResidentService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResidentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Resident>> GetAllResidentsAsync()
    {
        return await _unitOfWork.Residents.GetAllAsync();
    }
}
