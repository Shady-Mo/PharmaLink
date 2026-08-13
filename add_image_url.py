import re
dto_path = r"d:\ITI Graduation Project\PharmaLink-Backend\Application\DTOs\RecurringPrescription\RecurringResponseDto.cs"
service_path = r"d:\ITI Graduation Project\PharmaLink-Backend\Infrastructure\Services\RecurringPrescriptionService.cs"

# Update DTO
with open(dto_path, 'r', encoding='utf-8') as f:
    dto_content = f.read()

if "PrescriptionImageUrl" not in dto_content:
    dto_content = dto_content.replace(
        "public DateTime CreatedAt { get; set; }",
        "public DateTime CreatedAt { get; set; }\n    public string? PrescriptionImageUrl { get; set; }"
    )
    with open(dto_path, 'w', encoding='utf-8') as f:
        f.write(dto_content)

# Update Service
with open(service_path, 'r', encoding='utf-8') as f:
    svc_content = f.read()

# Add Include
svc_content = svc_content.replace(
    ".Include(x => x.Runs)",
    ".Include(x => x.Runs).Include(x => x.Prescription)"
)

# Add Mapping
svc_content = svc_content.replace(
    "CreatedAt = x.CreatedAt,",
    "CreatedAt = x.CreatedAt,\n            PrescriptionImageUrl = x.Prescription != null ? x.Prescription.ImageUrl : null,"
)
svc_content = svc_content.replace(
    "CreatedAt = recurring.CreatedAt,",
    "CreatedAt = recurring.CreatedAt,\n            PrescriptionImageUrl = recurring.Prescription?.ImageUrl,"
)

with open(service_path, 'w', encoding='utf-8') as f:
    f.write(svc_content)

print("Updated backend DTO and Service")
