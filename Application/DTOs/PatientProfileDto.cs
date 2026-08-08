using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class PatientProfileDto
{
	public Guid PatientId { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public string? ProfilePictureUrl { get; set; }
	public List<PatientAddressDto> Addresses { get; set; } = [];
}

public class PatientAddressDto
{
	public Guid AddressId { get; set; }

	//public string Label { get; set; } = string.Empty;
	public string AddressLine { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string Governorate { get; set; } = string.Empty;
	public bool IsDefault { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
}