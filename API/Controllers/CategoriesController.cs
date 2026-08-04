using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Infrastructure.Data;
using Application.DTOs.Drug.Responses;

namespace API.Controllers;

public class CategoriesController(AppDbContext context) : BaseApiController
{
    [HttpGet("")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var categories = await context.DrugCategories
            .AsNoTracking()
            .Include(c => c.SubCategories)
            .OrderBy(c => c.Level)
            .ThenBy(c => c.NameEn)
            .Select(c => new DrugCategoryDto
            {
                Id = c.Id,
                NameEn = c.NameEn,
                NameAr = c.NameAr,
                Slug = c.Slug,
                Level = c.Level,
                ParentId = c.ParentId
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("level/{level}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesByLevel(int level, CancellationToken cancellationToken)
    {
        var categories = await context.DrugCategories
            .AsNoTracking()
            .Where(c => c.Level == level)
            .OrderBy(c => c.NameEn)
            .Select(c => new DrugCategoryDto
            {
                Id = c.Id,
                NameEn = c.NameEn,
                NameAr = c.NameAr,
                Slug = c.Slug,
                Level = c.Level,
                ParentId = c.ParentId
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id}/subcategories")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubCategories(int id, CancellationToken cancellationToken)
    {
        var categories = await context.DrugCategories
            .AsNoTracking()
            .Where(c => c.ParentId == id)
            .OrderBy(c => c.NameEn)
            .Select(c => new DrugCategoryDto
            {
                Id = c.Id,
                NameEn = c.NameEn,
                NameAr = c.NameAr,
                Slug = c.Slug,
                Level = c.Level,
                ParentId = c.ParentId
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}
