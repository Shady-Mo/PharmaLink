using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Application.DTOs.Patient.Responses;
using Infrastructure.Data;
using Mapster;
using Domain.Entities;

var builder = new DbContextOptionsBuilder<AppDbContext>();
builder.UseSqlServer("Server=db58883.public.databaseasp.net; Database=db58883; User Id=db58883; Password=4e%ZT=8hbK+7; Encrypt=True; TrustServerCertificate=True;");
using var context = new AppDbContext(builder.Options);

var cart = context.Carts
    .Include(c => c.Items)
        .ThenInclude(ci => ci.Drug)
    .FirstOrDefault(c => c.PatientUserId == Guid.Parse("3954a50f-0847-4388-4407-08def08e9f03"));

if (cart != null) {
    foreach (var item in cart.Items) {
        Console.WriteLine($"Item: {item.Drug.BrandName}, Requires: {item.Drug.RequiresPrescription}");
    }
}
