﻿using AutoMapper;
using FAKA.Server.Auth;
using FAKA.Server.Data;
using FAKA.Server.Models;
using FAKA.Server.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FAKA.Server.Controllers.Admin;

[Route("api/v1/admin/[controller]")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper)
    {
        // 依赖注入
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    // GET: api/v1/admin/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProduct()
    {
        //only get the products that are not disabled and hidden
        var products = await _context.Product.Where(p => p.IsEnabled == true && p.IsHidden == false).ToListAsync();
        return Ok(products);
    }

    // GET: api/v1/admin/Products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Product.FindAsync(id);

        if (product == null) return NotFound("产品不存在");

        return Ok(product);
    }

    // PUT: api/v1/admin/Products/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id:int}")]
    public async Task<ActionResult> PutProduct(int id, ProductInDto productInDto)
    {
        var product = await _context.Product.FindAsync(id);
        if (product == null) return NotFound("产品不存在");
        _mapper.Map(productInDto, product);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id)) return NotFound();
            throw;
        }

        return Ok();
    }

    // POST: api/v1/admin/Products
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult> PostProduct(ProductInDto productInDto)
    {
        var product = _mapper.Map<Product>(productInDto);
        var category = await _context.ProductGroup.FindAsync(product.ProductGroupId);
        if (category == null) return BadRequest("商品分类不存在");
        _context.Product.Add(product);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // DELETE: api/v1/admin/Products/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await _context.Product.FindAsync(id);
        if (product == null) return NotFound();

        _context.Product.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(int id)
    {
        return (_context.Product?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}