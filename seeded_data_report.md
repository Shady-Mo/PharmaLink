# PharmaLink Real Egyptian Seeded Test Data Reference

> **Password Hash:** `AQAAAAIAAYagAAAAEOVBJ6YDOM+QMxbd22j+3OCaVtgxrD3HlAyPaIRpbvQRPaSVtGxY3mICJGiUr5Qpdg==`  
> **Plaintext Password:** `P@ss1234`

---
## Table of Contents

1. Users (25 accounts across 5 roles)
2. AspNetUserRoles
3. Pharmacies (30)
4. PharmacyBranches (90, 3 per pharmacy)
5. PharmacyBranchSchedules (630)
6. Addresses (10, 2 per patient)
7. PharmacistAssignments (10 = 5 active + 5 historical)
8. PharmacyInventories (all branches × all drugs)
9. Carts (5) & CartItems (12)
10. Orders (25) & OrderItems (~75)
11. OrderFulfillmentLegs (25) & StatusAudits (60+)
12. PrescriptionReviews (15) & Medicines (38+)
13. MedicalInquiries (20)
14. PhoneVerificationOtps (5)
15. RefreshTokens (25)

---
## 1. AspNetUsers — 25 Users Across 5 Roles

### 👤 Patients
| GUID | Full Name | Email | Phone |
| :--- | :--- | :--- | :--- |
| `11111111-0000-0000-0000-000000000001` | **Ahmed Mahmoud El-Sayed** | `patient1@pharmalink.eg` | `+201011111101` |
| `11111111-0000-0000-0000-000000000002` | **Sara Hassan Ibrahim** | `patient2@pharmalink.eg` | `+201011111102` |
| `11111111-0000-0000-0000-000000000003` | **Mohamed Ali Abdel-Rahman** | `patient3@pharmalink.eg` | `+201011111103` |
| `11111111-0000-0000-0000-000000000004` | **Mona Omar Mostafa** | `patient4@pharmalink.eg` | `+201011111104` |
| `11111111-0000-0000-0000-000000000005` | **Youssef Khaled Tarek** | `patient5@pharmalink.eg` | `+201011111105` |

### 👨‍⚕️ Pharmacists
| GUID | Full Name | Email | Phone |
| :--- | :--- | :--- | :--- |
| `22222222-0000-0000-0000-000000000001` | **Dr. Khaled Mansour El-Din** | `pharmacist1@pharmalink.eg` | `+201022222201` |
| `22222222-0000-0000-0000-000000000002` | **Dr. Hoda Youssef Fathy** | `pharmacist2@pharmalink.eg` | `+201022222202` |
| `22222222-0000-0000-0000-000000000003` | **Dr. Amr Nabil Gamal** | `pharmacist3@pharmalink.eg` | `+201022222203` |
| `22222222-0000-0000-0000-000000000004` | **Dr. Rania Abdel-Aziz** | `pharmacist4@pharmalink.eg` | `+201022222204` |
| `22222222-0000-0000-0000-000000000005` | **Dr. Yasser Ahmed Fouad** | `pharmacist5@pharmalink.eg` | `+201022222205` |

### 🏥 Pharmacy Admins
| GUID | Full Name | Email | Phone | Managed Pharmacy GUID | SuperAdmin |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `33333333-0000-0000-0000-000000000001` | **Pharmacy Admin Ezaby** | `pharmadmin1@pharmalink.eg` | `+201033333301` | `A0000000-0000-0000-0000-000000000001` | Yes |
| `33333333-0000-0000-0000-000000000002` | **Pharmacy Admin Seif** | `pharmadmin2@pharmalink.eg` | `+201033333302` | `A0000000-0000-0000-0000-000000000002` | No |
| `33333333-0000-0000-0000-000000000003` | **Pharmacy Admin Delmar** | `pharmadmin3@pharmalink.eg` | `+201033333303` | `A0000000-0000-0000-0000-000000000003` | No |
| `33333333-0000-0000-0000-000000000004` | **Pharmacy Admin 19011** | `pharmadmin4@pharmalink.eg` | `+201033333304` | `A0000000-0000-0000-0000-000000000004` | No |
| `33333333-0000-0000-0000-000000000005` | **Pharmacy Admin Misr** | `pharmadmin5@pharmalink.eg` | `+201033333305` | `A0000000-0000-0000-0000-000000000005` | No |

### ⚙️ System Admins
| GUID | Full Name | Email | Phone |
| :--- | :--- | :--- | :--- |
| `44444444-0000-0000-0000-000000000001` | **System Admin Core** | `admin1@pharmalink.eg` | `+201044444401` |
| `44444444-0000-0000-0000-000000000002` | **System Admin Operations** | `admin2@pharmalink.eg` | `+201044444402` |
| `44444444-0000-0000-0000-000000000003` | **System Admin Audit** | `admin3@pharmalink.eg` | `+201044444403` |
| `44444444-0000-0000-0000-000000000004` | **System Admin Technical** | `admin4@pharmalink.eg` | `+201044444404` |
| `44444444-0000-0000-0000-000000000005` | **System Admin Governance** | `admin5@pharmalink.eg` | `+201044444405` |

### 📋 Prescription Review Team
| GUID | Full Name | Email | Phone |
| :--- | :--- | :--- | :--- |
| `55555555-0000-0000-0000-000000000001` | **Dr. Tarek El-Kashif** | `reviewteam1@pharmalink.eg` | `+201055555501` |
| `55555555-0000-0000-0000-000000000002` | **Dr. Nouran Sherif** | `reviewteam2@pharmalink.eg` | `+201055555502` |
| `55555555-0000-0000-0000-000000000003` | **Dr. Kareem Abdel-Moneim** | `reviewteam3@pharmalink.eg` | `+201055555503` |
| `55555555-0000-0000-0000-000000000004` | **Dr. Salma El-Ghazaly** | `reviewteam4@pharmalink.eg` | `+201055555504` |
| `55555555-0000-0000-0000-000000000005` | **Dr. Hesham El-Naggar** | `reviewteam5@pharmalink.eg` | `+201055555505` |

---
## 2. AspNetUserRoles
| User | Role |
| :--- | :--- |
| `11111111-0000-0000-0000-000000000001` (Ahmed Mahmoud El-Sayed) | Patient |
| `11111111-0000-0000-0000-000000000002` (Sara Hassan Ibrahim) | Patient |
| `11111111-0000-0000-0000-000000000003` (Mohamed Ali Abdel-Rahman) | Patient |
| `11111111-0000-0000-0000-000000000004` (Mona Omar Mostafa) | Patient |
| `11111111-0000-0000-0000-000000000005` (Youssef Khaled Tarek) | Patient |
| `22222222-0000-0000-0000-000000000001` (Dr. Khaled Mansour El-Din) | Pharmacist |
| `22222222-0000-0000-0000-000000000002` (Dr. Hoda Youssef Fathy) | Pharmacist |
| `22222222-0000-0000-0000-000000000003` (Dr. Amr Nabil Gamal) | Pharmacist |
| `22222222-0000-0000-0000-000000000004` (Dr. Rania Abdel-Aziz) | Pharmacist |
| `22222222-0000-0000-0000-000000000005` (Dr. Yasser Ahmed Fouad) | Pharmacist |
| `33333333-0000-0000-0000-000000000001` (Pharmacy Admin Ezaby) | PharmacyAdmin |
| `33333333-0000-0000-0000-000000000002` (Pharmacy Admin Seif) | PharmacyAdmin |
| `33333333-0000-0000-0000-000000000003` (Pharmacy Admin Delmar) | PharmacyAdmin |
| `33333333-0000-0000-0000-000000000004` (Pharmacy Admin 19011) | PharmacyAdmin |
| `33333333-0000-0000-0000-000000000005` (Pharmacy Admin Misr) | PharmacyAdmin |
| `44444444-0000-0000-0000-000000000001` (System Admin Core) | Admin |
| `44444444-0000-0000-0000-000000000002` (System Admin Operations) | Admin |
| `44444444-0000-0000-0000-000000000003` (System Admin Audit) | Admin |
| `44444444-0000-0000-0000-000000000004` (System Admin Technical) | Admin |
| `44444444-0000-0000-0000-000000000005` (System Admin Governance) | Admin |
| `55555555-0000-0000-0000-000000000001` (Dr. Tarek El-Kashif) | PrescriptionReviewTeam |
| `55555555-0000-0000-0000-000000000002` (Dr. Nouran Sherif) | PrescriptionReviewTeam |
| `55555555-0000-0000-0000-000000000003` (Dr. Kareem Abdel-Moneim) | PrescriptionReviewTeam |
| `55555555-0000-0000-0000-000000000004` (Dr. Salma El-Ghazaly) | PrescriptionReviewTeam |
| `55555555-0000-0000-0000-000000000005` (Dr. Hesham El-Naggar) | PrescriptionReviewTeam |

---
## 3. Pharmacies (30)
| Pharmacy GUID | Name | License | Verification Status |
| :--- | :--- | :--- | :--- |
| `A0000000-0000-0000-0000-000000000001` | **El-Ezaby Pharmacy Chain** | `LIC-10001` | Verified (2) |
| `A0000000-0000-0000-0000-000000000002` | **Seif Pharmacies Group** | `LIC-10002` | Verified (2) |
| `A0000000-0000-0000-0000-000000000003` | **Delmar & Attalla** | `LIC-10003` | Verified (2) |
| `A0000000-0000-0000-0000-000000000004` | **19011 Pharmacies** | `LIC-10004` | Verified (2) |
| `A0000000-0000-0000-0000-000000000005` | **Misr Pharmacies** | `LIC-10005` | Pending (1) |
| `A0000000-0000-0000-0000-000000000006` | **Al-Dawaa Pharmacies** | `LIC-10006` | Verified (2) |
| `A0000000-0000-0000-0000-000000000007` | **Rushdy Pharmacies** | `LIC-10007` | Verified (2) |
| `A0000000-0000-0000-0000-000000000008` | **Care Pharmacies** | `LIC-10008` | Verified (2) |
| `A0000000-0000-0000-0000-000000000009` | **Al-Eman Pharmacy Group** | `LIC-10009` | Verified (2) |
| `A0000000-0000-0000-0000-000000000010` | **Al-Shifa Pharmacies** | `LIC-10010` | Verified (2) |
| `A0000000-0000-0000-0000-000000000011` | **Nour Pharmacy Chain** | `LIC-10011` | Verified (2) |
| `A0000000-0000-0000-0000-000000000012` | **Al-Ahram Pharmacies** | `LIC-10012` | Verified (2) |
| `A0000000-0000-0000-0000-000000000013` | **El-Tahrir Pharmacy Group** | `LIC-10013` | Verified (2) |
| `A0000000-0000-0000-0000-000000000014` | **Al-Safwa Pharmacies** | `LIC-10014` | Verified (2) |
| `A0000000-0000-0000-0000-000000000015` | **El-Hayah Pharmacy Chain** | `LIC-10015` | Verified (2) |
| `A0000000-0000-0000-0000-000000000016` | **Al-Amal Pharmacies** | `LIC-10016` | Verified (2) |
| `A0000000-0000-0000-0000-000000000017` | **Al-Hikma Pharmacy Group** | `LIC-10017` | Verified (2) |
| `A0000000-0000-0000-0000-000000000018` | **El-Salam Pharmacies** | `LIC-10018` | Verified (2) |
| `A0000000-0000-0000-0000-000000000019` | **Al-Ghad Pharmacy Chain** | `LIC-10019` | Verified (2) |
| `A0000000-0000-0000-0000-000000000020` | **El-Nile Pharmacies** | `LIC-10020` | Verified (2) |
| `A0000000-0000-0000-0000-000000000021` | **Al-Basha Pharmacy Group** | `LIC-10021` | Verified (2) |
| `A0000000-0000-0000-0000-000000000022` | **El-Shorouk Pharmacies** | `LIC-10022` | Verified (2) |
| `A0000000-0000-0000-0000-000000000023` | **Al-Zahraa Pharmacy Chain** | `LIC-10023` | Verified (2) |
| `A0000000-0000-0000-0000-000000000024` | **El-Khedawi Pharmacies** | `LIC-10024` | Verified (2) |
| `A0000000-0000-0000-0000-000000000025` | **Al-Rawda Pharmacy Group** | `LIC-10025` | Verified (2) |
| `A0000000-0000-0000-0000-000000000026` | **El-Fayrouz Pharmacies** | `LIC-10026` | Verified (2) |
| `A0000000-0000-0000-0000-000000000027` | **Al-Nasr Pharmacy Chain** | `LIC-10027` | Verified (2) |
| `A0000000-0000-0000-0000-000000000028` | **El-Eman Medical Pharmacies** | `LIC-10028` | Verified (2) |
| `A0000000-0000-0000-0000-000000000029` | **Al-Farabi Pharmacy Group** | `LIC-10029` | Verified (2) |
| `A0000000-0000-0000-0000-000000000030` | **El-Rowad Pharmacies** | `LIC-10030` | Verified (2) |

---
## 4. PharmacyBranches (90 — 3 per pharmacy)
| Branch GUID | Pharmacy | Branch Name | City | Governorate |
| :--- | :--- | :--- | :--- | :--- |
| `B0000000-0000-0000-0000-000000000001` | `A0000000-0000-0000-0000-000000000001` | **El-Ezaby Dokki Branch** | Dokki | Giza |
| `B0000000-0000-0000-0000-000000000002` | `A0000000-0000-0000-0000-000000000001` | **El-Ezaby Nasr City Branch** | Nasr City | Cairo |
| `B0000000-0000-0000-0000-000000000003` | `A0000000-0000-0000-0000-000000000001` | **El-Ezaby Maadi Branch** | Maadi | Cairo |
| `B0000000-0000-0000-0000-000000000004` | `A0000000-0000-0000-0000-000000000002` | **Seif Heliopolis Branch** | Heliopolis | Cairo |
| `B0000000-0000-0000-0000-000000000005` | `A0000000-0000-0000-0000-000000000002` | **Seif Mohandessin Branch** | Mohandessin | Giza |
| `B0000000-0000-0000-0000-000000000006` | `A0000000-0000-0000-0000-000000000002` | **Seif Smouha Branch** | Smouha | Alexandria |
| `B0000000-0000-0000-0000-000000000007` | `A0000000-0000-0000-0000-000000000003` | **Delmar Zamalek Branch** | Zamalek | Cairo |
| `B0000000-0000-0000-0000-000000000008` | `A0000000-0000-0000-0000-000000000003` | **Delmar New Cairo Branch** | New Cairo | Cairo |
| `B0000000-0000-0000-0000-000000000009` | `A0000000-0000-0000-0000-000000000003` | **Delmar Al-Attarin Branch** | Al-Attarin | Alexandria |
| `B0000000-0000-0000-0000-000000000010` | `A0000000-0000-0000-0000-000000000004` | **19011 Sheikh Zayed Branch** | Sheikh Zayed | Giza |
| `B0000000-0000-0000-0000-000000000011` | `A0000000-0000-0000-0000-000000000004` | **19011 Mansoura Branch** | Mansoura | Dakahlia |
| `B0000000-0000-0000-0000-000000000012` | `A0000000-0000-0000-0000-000000000004` | **19011 Tanta Branch** | Tanta | Gharbia |
| `B0000000-0000-0000-0000-000000000013` | `A0000000-0000-0000-0000-000000000005` | **Misr Downtown Branch** | Downtown | Cairo |
| `B0000000-0000-0000-0000-000000000014` | `A0000000-0000-0000-0000-000000000005` | **Misr Haram Branch** | Haram | Giza |
| `B0000000-0000-0000-0000-000000000015` | `A0000000-0000-0000-0000-000000000005` | **Misr Zagazig Branch** | Zagazig | Sharqia |
| `B0000000-0000-0000-0000-000000000016` | `A0000000-0000-0000-0000-000000000006` | **Al-Dawaa Asyut Branch** | Asyut | Asyut |
| `B0000000-0000-0000-0000-000000000017` | `A0000000-0000-0000-0000-000000000006` | **Al-Dawaa Shubra Branch** | Shubra | Cairo |
| `B0000000-0000-0000-0000-000000000018` | `A0000000-0000-0000-0000-000000000006` | **Al-Dawaa Ismailia Branch** | Ismailia | Ismailia |
| `B0000000-0000-0000-0000-000000000019` | `A0000000-0000-0000-0000-000000000007` | **Rushdy Port Said Branch** | Port Said | Port Said |
| `B0000000-0000-0000-0000-000000000020` | `A0000000-0000-0000-0000-000000000007` | **Rushdy Suez Branch** | Suez | Suez |
| `B0000000-0000-0000-0000-000000000021` | `A0000000-0000-0000-0000-000000000007` | **Rushdy Fayoum Branch** | Fayoum | Fayoum |
| `B0000000-0000-0000-0000-000000000022` | `A0000000-0000-0000-0000-000000000008` | **Care Aswan Branch** | Aswan | Aswan |
| `B0000000-0000-0000-0000-000000000023` | `A0000000-0000-0000-0000-000000000008` | **Care Hurghada Branch** | Hurghada | Red Sea |
| `B0000000-0000-0000-0000-000000000024` | `A0000000-0000-0000-0000-000000000008` | **Care Luxor Branch** | Luxor | Luxor |
| `B0000000-0000-0000-0000-000000000025` | `A0000000-0000-0000-0000-000000000009` | **Al-Eman Minya Branch** | Minya | Minya |
| `B0000000-0000-0000-0000-000000000026` | `A0000000-0000-0000-0000-000000000009` | **Al-Eman Beni Suef Branch** | Beni Suef | Beni Suef |
| `B0000000-0000-0000-0000-000000000027` | `A0000000-0000-0000-0000-000000000009` | **Al-Eman Damanhour Branch** | Damanhour | Beheira |
| `B0000000-0000-0000-0000-000000000028` | `A0000000-0000-0000-0000-000000000010` | **Al-Shifa Shibin El Kom Branch** | Shibin El Kom | Monufia |
| `B0000000-0000-0000-0000-000000000029` | `A0000000-0000-0000-0000-000000000010` | **Al-Shifa Banha Branch** | Banha | Qalyubia |
| `B0000000-0000-0000-0000-000000000030` | `A0000000-0000-0000-0000-000000000010` | **Al-Shifa Damietta Branch** | Damietta | Damietta |
| `B0000000-0000-0000-0000-000000000031` | `A0000000-0000-0000-0000-000000000011` | **Nour Dokki Branch** | Dokki | Giza |
| `B0000000-0000-0000-0000-000000000032` | `A0000000-0000-0000-0000-000000000011` | **Nour Nasr City Branch** | Nasr City | Cairo |
| `B0000000-0000-0000-0000-000000000033` | `A0000000-0000-0000-0000-000000000011` | **Nour Maadi Branch** | Maadi | Cairo |
| `B0000000-0000-0000-0000-000000000034` | `A0000000-0000-0000-0000-000000000012` | **Al-Ahram Heliopolis Branch** | Heliopolis | Cairo |
| `B0000000-0000-0000-0000-000000000035` | `A0000000-0000-0000-0000-000000000012` | **Al-Ahram Mohandessin Branch** | Mohandessin | Giza |
| `B0000000-0000-0000-0000-000000000036` | `A0000000-0000-0000-0000-000000000012` | **Al-Ahram Smouha Branch** | Smouha | Alexandria |
| `B0000000-0000-0000-0000-000000000037` | `A0000000-0000-0000-0000-000000000013` | **El-Tahrir Zamalek Branch** | Zamalek | Cairo |
| `B0000000-0000-0000-0000-000000000038` | `A0000000-0000-0000-0000-000000000013` | **El-Tahrir New Cairo Branch** | New Cairo | Cairo |
| `B0000000-0000-0000-0000-000000000039` | `A0000000-0000-0000-0000-000000000013` | **El-Tahrir Al-Attarin Branch** | Al-Attarin | Alexandria |
| `B0000000-0000-0000-0000-000000000040` | `A0000000-0000-0000-0000-000000000014` | **Al-Safwa Sheikh Zayed Branch** | Sheikh Zayed | Giza |
| `B0000000-0000-0000-0000-000000000041` | `A0000000-0000-0000-0000-000000000014` | **Al-Safwa Mansoura Branch** | Mansoura | Dakahlia |
| `B0000000-0000-0000-0000-000000000042` | `A0000000-0000-0000-0000-000000000014` | **Al-Safwa Tanta Branch** | Tanta | Gharbia |
| `B0000000-0000-0000-0000-000000000043` | `A0000000-0000-0000-0000-000000000015` | **El-Hayah Downtown Branch** | Downtown | Cairo |
| `B0000000-0000-0000-0000-000000000044` | `A0000000-0000-0000-0000-000000000015` | **El-Hayah Haram Branch** | Haram | Giza |
| `B0000000-0000-0000-0000-000000000045` | `A0000000-0000-0000-0000-000000000015` | **El-Hayah Zagazig Branch** | Zagazig | Sharqia |
| `B0000000-0000-0000-0000-000000000046` | `A0000000-0000-0000-0000-000000000016` | **Al-Amal Asyut Branch** | Asyut | Asyut |
| `B0000000-0000-0000-0000-000000000047` | `A0000000-0000-0000-0000-000000000016` | **Al-Amal Shubra Branch** | Shubra | Cairo |
| `B0000000-0000-0000-0000-000000000048` | `A0000000-0000-0000-0000-000000000016` | **Al-Amal Ismailia Branch** | Ismailia | Ismailia |
| `B0000000-0000-0000-0000-000000000049` | `A0000000-0000-0000-0000-000000000017` | **Al-Hikma Port Said Branch** | Port Said | Port Said |
| `B0000000-0000-0000-0000-000000000050` | `A0000000-0000-0000-0000-000000000017` | **Al-Hikma Suez Branch** | Suez | Suez |
| `B0000000-0000-0000-0000-000000000051` | `A0000000-0000-0000-0000-000000000017` | **Al-Hikma Fayoum Branch** | Fayoum | Fayoum |
| `B0000000-0000-0000-0000-000000000052` | `A0000000-0000-0000-0000-000000000018` | **El-Salam Aswan Branch** | Aswan | Aswan |
| `B0000000-0000-0000-0000-000000000053` | `A0000000-0000-0000-0000-000000000018` | **El-Salam Hurghada Branch** | Hurghada | Red Sea |
| `B0000000-0000-0000-0000-000000000054` | `A0000000-0000-0000-0000-000000000018` | **El-Salam Luxor Branch** | Luxor | Luxor |
| `B0000000-0000-0000-0000-000000000055` | `A0000000-0000-0000-0000-000000000019` | **Al-Ghad Minya Branch** | Minya | Minya |
| `B0000000-0000-0000-0000-000000000056` | `A0000000-0000-0000-0000-000000000019` | **Al-Ghad Beni Suef Branch** | Beni Suef | Beni Suef |
| `B0000000-0000-0000-0000-000000000057` | `A0000000-0000-0000-0000-000000000019` | **Al-Ghad Damanhour Branch** | Damanhour | Beheira |
| `B0000000-0000-0000-0000-000000000058` | `A0000000-0000-0000-0000-000000000020` | **El-Nile Shibin El Kom Branch** | Shibin El Kom | Monufia |
| `B0000000-0000-0000-0000-000000000059` | `A0000000-0000-0000-0000-000000000020` | **El-Nile Banha Branch** | Banha | Qalyubia |
| `B0000000-0000-0000-0000-000000000060` | `A0000000-0000-0000-0000-000000000020` | **El-Nile Damietta Branch** | Damietta | Damietta |
| `B0000000-0000-0000-0000-000000000061` | `A0000000-0000-0000-0000-000000000021` | **Al-Basha Dokki Branch** | Dokki | Giza |
| `B0000000-0000-0000-0000-000000000062` | `A0000000-0000-0000-0000-000000000021` | **Al-Basha Nasr City Branch** | Nasr City | Cairo |
| `B0000000-0000-0000-0000-000000000063` | `A0000000-0000-0000-0000-000000000021` | **Al-Basha Maadi Branch** | Maadi | Cairo |
| `B0000000-0000-0000-0000-000000000064` | `A0000000-0000-0000-0000-000000000022` | **El-Shorouk Heliopolis Branch** | Heliopolis | Cairo |
| `B0000000-0000-0000-0000-000000000065` | `A0000000-0000-0000-0000-000000000022` | **El-Shorouk Mohandessin Branch** | Mohandessin | Giza |
| `B0000000-0000-0000-0000-000000000066` | `A0000000-0000-0000-0000-000000000022` | **El-Shorouk Smouha Branch** | Smouha | Alexandria |
| `B0000000-0000-0000-0000-000000000067` | `A0000000-0000-0000-0000-000000000023` | **Al-Zahraa Zamalek Branch** | Zamalek | Cairo |
| `B0000000-0000-0000-0000-000000000068` | `A0000000-0000-0000-0000-000000000023` | **Al-Zahraa New Cairo Branch** | New Cairo | Cairo |
| `B0000000-0000-0000-0000-000000000069` | `A0000000-0000-0000-0000-000000000023` | **Al-Zahraa Al-Attarin Branch** | Al-Attarin | Alexandria |
| `B0000000-0000-0000-0000-000000000070` | `A0000000-0000-0000-0000-000000000024` | **El-Khedawi Sheikh Zayed Branch** | Sheikh Zayed | Giza |
| `B0000000-0000-0000-0000-000000000071` | `A0000000-0000-0000-0000-000000000024` | **El-Khedawi Mansoura Branch** | Mansoura | Dakahlia |
| `B0000000-0000-0000-0000-000000000072` | `A0000000-0000-0000-0000-000000000024` | **El-Khedawi Tanta Branch** | Tanta | Gharbia |
| `B0000000-0000-0000-0000-000000000073` | `A0000000-0000-0000-0000-000000000025` | **Al-Rawda Downtown Branch** | Downtown | Cairo |
| `B0000000-0000-0000-0000-000000000074` | `A0000000-0000-0000-0000-000000000025` | **Al-Rawda Haram Branch** | Haram | Giza |
| `B0000000-0000-0000-0000-000000000075` | `A0000000-0000-0000-0000-000000000025` | **Al-Rawda Zagazig Branch** | Zagazig | Sharqia |
| `B0000000-0000-0000-0000-000000000076` | `A0000000-0000-0000-0000-000000000026` | **El-Fayrouz Asyut Branch** | Asyut | Asyut |
| `B0000000-0000-0000-0000-000000000077` | `A0000000-0000-0000-0000-000000000026` | **El-Fayrouz Shubra Branch** | Shubra | Cairo |
| `B0000000-0000-0000-0000-000000000078` | `A0000000-0000-0000-0000-000000000026` | **El-Fayrouz Ismailia Branch** | Ismailia | Ismailia |
| `B0000000-0000-0000-0000-000000000079` | `A0000000-0000-0000-0000-000000000027` | **Al-Nasr Port Said Branch** | Port Said | Port Said |
| `B0000000-0000-0000-0000-000000000080` | `A0000000-0000-0000-0000-000000000027` | **Al-Nasr Suez Branch** | Suez | Suez |
| `B0000000-0000-0000-0000-000000000081` | `A0000000-0000-0000-0000-000000000027` | **Al-Nasr Fayoum Branch** | Fayoum | Fayoum |
| `B0000000-0000-0000-0000-000000000082` | `A0000000-0000-0000-0000-000000000028` | **El-Eman Aswan Branch** | Aswan | Aswan |
| `B0000000-0000-0000-0000-000000000083` | `A0000000-0000-0000-0000-000000000028` | **El-Eman Hurghada Branch** | Hurghada | Red Sea |
| `B0000000-0000-0000-0000-000000000084` | `A0000000-0000-0000-0000-000000000028` | **El-Eman Luxor Branch** | Luxor | Luxor |
| `B0000000-0000-0000-0000-000000000085` | `A0000000-0000-0000-0000-000000000029` | **Al-Farabi Minya Branch** | Minya | Minya |
| `B0000000-0000-0000-0000-000000000086` | `A0000000-0000-0000-0000-000000000029` | **Al-Farabi Beni Suef Branch** | Beni Suef | Beni Suef |
| `B0000000-0000-0000-0000-000000000087` | `A0000000-0000-0000-0000-000000000029` | **Al-Farabi Damanhour Branch** | Damanhour | Beheira |
| `B0000000-0000-0000-0000-000000000088` | `A0000000-0000-0000-0000-000000000030` | **El-Rowad Shibin El Kom Branch** | Shibin El Kom | Monufia |
| `B0000000-0000-0000-0000-000000000089` | `A0000000-0000-0000-0000-000000000030` | **El-Rowad Banha Branch** | Banha | Qalyubia |
| `B0000000-0000-0000-0000-000000000090` | `A0000000-0000-0000-0000-000000000030` | **El-Rowad Damietta Branch** | Damietta | Damietta |

---
## 5. PharmacyBranchSchedules (630)
7 schedule rows per branch. Sun–Thu: 08:00–23:59. Fri: 12:00–23:00. Sat: 09:00–22:00.

---
## 6. Addresses (10 — 2 per patient)
| Address GUID | Patient | City | Governorate | Default |
| :--- | :--- | :--- | :--- | :--- |
| `C0000000-0000-0000-0000-000000000001` | Ahmed Mahmoud El-Sayed | Dokki | Giza | Yes |
| `C0000000-0000-0000-0000-000000000002` | Ahmed Mahmoud El-Sayed | Maadi | Cairo | No |
| `C0000000-0000-0000-0000-000000000003` | Sara Hassan Ibrahim | Nasr City | Cairo | Yes |
| `C0000000-0000-0000-0000-000000000004` | Sara Hassan Ibrahim | Zamalek | Cairo | No |
| `C0000000-0000-0000-0000-000000000005` | Mohamed Ali Abdel-Rahman | Smouha | Alexandria | Yes |
| `C0000000-0000-0000-0000-000000000006` | Mohamed Ali Abdel-Rahman | Smouha | Alexandria | No |
| `C0000000-0000-0000-0000-000000000007` | Mona Omar Mostafa | Mansoura | Dakahlia | Yes |
| `C0000000-0000-0000-0000-000000000008` | Mona Omar Mostafa | Gleem | Alexandria | No |
| `C0000000-0000-0000-0000-000000000009` | Youssef Khaled Tarek | Tanta | Gharbia | Yes |
| `C0000000-0000-0000-0000-000000000010` | Youssef Khaled Tarek | Mansoura | Dakahlia | No |

---
## 7. PharmacistAssignments (10)
| ID | Pharmacist | Pharmacy | Branch | Admin | Active |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `E0000000-0000-0000-0000-000000000001` | Dr. Khaled Mansour El-Din | El-Ezaby Pharmacy Chain | `B0000000-0000-0000-0000-000000000001` | `33333333-0000-0000-0000-000000000001` | **Active** |
| `E0000000-0000-0000-0000-000000000002` | Dr. Khaled Mansour El-Din | El-Ezaby Pharmacy Chain | `B0000000-0000-0000-0000-000000000002` | `33333333-0000-0000-0000-000000000001` | Historical |
| `E0000000-0000-0000-0000-000000000003` | Dr. Hoda Youssef Fathy | Seif Pharmacies Group | `B0000000-0000-0000-0000-000000000004` | `33333333-0000-0000-0000-000000000002` | **Active** |
| `E0000000-0000-0000-0000-000000000004` | Dr. Hoda Youssef Fathy | Seif Pharmacies Group | `B0000000-0000-0000-0000-000000000005` | `33333333-0000-0000-0000-000000000002` | Historical |
| `E0000000-0000-0000-0000-000000000005` | Dr. Amr Nabil Gamal | Delmar & Attalla | `B0000000-0000-0000-0000-000000000007` | `33333333-0000-0000-0000-000000000003` | **Active** |
| `E0000000-0000-0000-0000-000000000006` | Dr. Amr Nabil Gamal | Delmar & Attalla | `B0000000-0000-0000-0000-000000000008` | `33333333-0000-0000-0000-000000000003` | Historical |
| `E0000000-0000-0000-0000-000000000007` | Dr. Rania Abdel-Aziz | 19011 Pharmacies | `B0000000-0000-0000-0000-000000000010` | `33333333-0000-0000-0000-000000000004` | **Active** |
| `E0000000-0000-0000-0000-000000000008` | Dr. Rania Abdel-Aziz | 19011 Pharmacies | `B0000000-0000-0000-0000-000000000011` | `33333333-0000-0000-0000-000000000004` | Historical |
| `E0000000-0000-0000-0000-000000000009` | Dr. Yasser Ahmed Fouad | Misr Pharmacies | `B0000000-0000-0000-0000-000000000013` | `33333333-0000-0000-0000-000000000005` | **Active** |
| `E0000000-0000-0000-0000-000000000010` | Dr. Yasser Ahmed Fouad | Misr Pharmacies | `B0000000-0000-0000-0000-000000000014` | `33333333-0000-0000-0000-000000000005` | Historical |

---
## 8. PharmacyInventories
All 90 branches × all drugs in catalog. StockQuantity varies: 0 (out), 5/8 (low), 15-300 (available). Random price 10–500 EGP, expiry 60–760 days ahead.

---
## 9. Carts & CartItems
| Cart GUID | Patient | Drug Variables | Qty |
| :--- | :--- | :--- | :--- |
| `F0000000-0000-0000-0000-000000000001` | Ahmed Mahmoud El-Sayed | @Drug1/2/3 | 2/1/3 |
| `F0000000-0000-0000-0000-000000000002` | Sara Hassan Ibrahim | @Drug1/2/3 | 2/1/3 |
| `F0000000-0000-0000-0000-000000000003` | Mohamed Ali Abdel-Rahman | @Drug1/2/3 | 2/1/3 |
| `F0000000-0000-0000-0000-000000000004` | Mona Omar Mostafa | @Drug1/2/3 | 2/1/3 |
| `F0000000-0000-0000-0000-000000000005` | Youssef Khaled Tarek | @Drug1/2/3 | 2/1/3 |

---
## 10. Orders (25) — Varied Statuses
| Order GUID | Patient | Status | Amount | Fulfillment |
| :--- | :--- | :--- | :--- | :--- |
| `O0000000-0000-0000-0000-000000000001` | Ahmed Mahmoud El-Sayed | Completed (4) | 470.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000002` | Sara Hassan Ibrahim | Completed (4) | 285.50 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000003` | Mohamed Ali Abdel-Rahman | Completed (4) | 195.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000004` | Mona Omar Mostafa | Completed (4) | 340.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000005` | Youssef Khaled Tarek | Completed (4) | 510.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000006` | Ahmed Mahmoud El-Sayed | Shipped (3) | 88.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000007` | Sara Hassan Ibrahim | Shipped (3) | 165.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000008` | Mohamed Ali Abdel-Rahman | Processing (2) | 220.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000009` | Mona Omar Mostafa | Processing (2) | 400.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000010` | Youssef Khaled Tarek | Processing (2) | 75.50 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000011` | Ahmed Mahmoud El-Sayed | Processing (2) | 130.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000012` | Sara Hassan Ibrahim | Pending (1) | 55.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000013` | Mohamed Ali Abdel-Rahman | Pending (1) | 260.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000014` | Mona Omar Mostafa | Cancelled (5) | 95.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000015` | Youssef Khaled Tarek | Cancelled (5) | 180.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000016` | Ahmed Mahmoud El-Sayed | Completed (4) | 330.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000017` | Sara Hassan Ibrahim | Completed (4) | 415.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000018` | Mohamed Ali Abdel-Rahman | Completed (4) | 200.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000019` | Mona Omar Mostafa | Shipped (3) | 350.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000020` | Youssef Khaled Tarek | Processing (2) | 125.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000021` | Ahmed Mahmoud El-Sayed | Pending (1) | 78.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000022` | Sara Hassan Ibrahim | Completed (4) | 490.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000023` | Mohamed Ali Abdel-Rahman | Cancelled (5) | 210.00 EGP | Pickup |
| `O0000000-0000-0000-0000-000000000024` | Mona Omar Mostafa | Processing (2) | 380.00 EGP | Delivery |
| `O0000000-0000-0000-0000-000000000025` | Youssef Khaled Tarek | Pending (1) | 60.00 EGP | Delivery |

---
## 11. OrderFulfillmentLegs & Status Audits
25 legs (one per order) with 1–4 audit entries each depending on leg progress.

---
## 12. PrescriptionReviews (15)
| Review GUID | Patient | Processing Status | Review Status |
| :--- | :--- | :--- | :--- |
| `R0000000-0000-0000-0000-000000000001` | Ahmed Mahmoud El-Sayed | Completed | Approved |
| `R0000000-0000-0000-0000-000000000002` | Sara Hassan Ibrahim | Completed | Approved |
| `R0000000-0000-0000-0000-000000000003` | Mohamed Ali Abdel-Rahman | Completed | Approved |
| `R0000000-0000-0000-0000-000000000004` | Mona Omar Mostafa | Completed | OrderCreated |
| `R0000000-0000-0000-0000-000000000005` | Youssef Khaled Tarek | Rejected | Rejected |
| `R0000000-0000-0000-0000-000000000006` | Ahmed Mahmoud El-Sayed | PendingPharmacistReview | PendingReview |
| `R0000000-0000-0000-0000-000000000007` | Sara Hassan Ibrahim | Processing | PendingReview |
| `R0000000-0000-0000-0000-000000000008` | Mohamed Ali Abdel-Rahman | NeedsPatientApproval | PendingReview |
| `R0000000-0000-0000-0000-000000000009` | Mona Omar Mostafa | Completed | Approved |
| `R0000000-0000-0000-0000-000000000010` | Youssef Khaled Tarek | Failed | PendingReview |
| `R0000000-0000-0000-0000-000000000011` | Ahmed Mahmoud El-Sayed | Completed | Approved |
| `R0000000-0000-0000-0000-000000000012` | Sara Hassan Ibrahim | Completed | Approved |
| `R0000000-0000-0000-0000-000000000013` | Mohamed Ali Abdel-Rahman | PendingPharmacistReview | PendingReview |
| `R0000000-0000-0000-0000-000000000014` | Mona Omar Mostafa | Processing | PendingReview |
| `R0000000-0000-0000-0000-000000000015` | Youssef Khaled Tarek | Completed | Approved |

---
## 13. MedicalInquiries (20)
| Inquiry GUID | Patient | Status | Question (truncated) |
| :--- | :--- | :--- | :--- |
| `M0000000-0000-0000-0000-000000000001` | Ahmed Mahmoud El-Sayed | **Answered** | Can I take Panadol Extra during pregnancy? |
| `M0000000-0000-0000-0000-000000000002` | Sara Hassan Ibrahim | **Answered** | What is the proper dosage timing for Augmentin 1g? |
| `M0000000-0000-0000-0000-000000000003` | Mohamed Ali Abdel-Rahman | **Answered** | Can Cataflam 50mg be taken with blood pressure medication? |
| `M0000000-0000-0000-0000-000000000004` | Mona Omar Mostafa | **Answered** | Is Glucophage 500mg safe for elderly patients? |
| `M0000000-0000-0000-0000-000000000005` | Youssef Khaled Tarek | **Answered** | What are the side effects of Zithromax 500mg? |
| `M0000000-0000-0000-0000-000000000006` | Ahmed Mahmoud El-Sayed | **Pending** | Can I take Omeprazole and Aspirin together? |
| `M0000000-0000-0000-0000-000000000007` | Sara Hassan Ibrahim | **Answered** | Is Nexium 40mg suitable for chronic GERD treatment? |
| `M0000000-0000-0000-0000-000000000008` | Mohamed Ali Abdel-Rahman | **Pending** | What is the difference between Concor 5 and Concor 10? |
| `M0000000-0000-0000-0000-000000000009` | Mona Omar Mostafa | **Answered** | Can children under 12 take Brufen 400mg? |
| `M0000000-0000-0000-0000-000000000010` | Youssef Khaled Tarek | **Answered** | How long can I store insulin in the fridge? |
| `M0000000-0000-0000-0000-000000000011` | Ahmed Mahmoud El-Sayed | **Pending** | Can I stop taking Atorvastatin 40mg suddenly? |
| `M0000000-0000-0000-0000-000000000012` | Sara Hassan Ibrahim | **Answered** | What is the half-life of Xanax 0.5mg? |
| `M0000000-0000-0000-0000-000000000013` | Mohamed Ali Abdel-Rahman | **Answered** | Does Amoxicillin treat viral infections? |
| `M0000000-0000-0000-0000-000000000014` | Mona Omar Mostafa | **Answered** | Is it safe to take Voltaren gel and oral Diclofenac together... |
| `M0000000-0000-0000-0000-000000000015` | Youssef Khaled Tarek | **Answered** | Can I drink alcohol while taking Metronidazole (Flagyl)? |
| `M0000000-0000-0000-0000-000000000016` | Ahmed Mahmoud El-Sayed | **Pending** | What is the correct storage temperature for Amoxil syrup? |
| `M0000000-0000-0000-0000-000000000017` | Sara Hassan Ibrahim | **Answered** | Is it safe to take Panadol Night during daytime? |
| `M0000000-0000-0000-0000-000000000018` | Mohamed Ali Abdel-Rahman | **Pending** | Can I use Nasonex spray during pregnancy? |
| `M0000000-0000-0000-0000-000000000019` | Mona Omar Mostafa | **Answered** | How do I know if my blood pressure medication is working? |
| `M0000000-0000-0000-0000-000000000020` | Youssef Khaled Tarek | **Answered** | What is the maximum daily dose of Vitamin D3? |

---
## 14. PhoneVerificationOtps (5)
| OTP GUID | User | Expires At |
| :--- | :--- | :--- |
| `P0000000-0000-0000-0000-000000000001` | Ahmed Mahmoud El-Sayed | +15 minutes from seeding |
| `P0000000-0000-0000-0000-000000000002` | Sara Hassan Ibrahim | +15 minutes from seeding |
| `P0000000-0000-0000-0000-000000000003` | Mohamed Ali Abdel-Rahman | +15 minutes from seeding |
| `P0000000-0000-0000-0000-000000000004` | Mona Omar Mostafa | +15 minutes from seeding |
| `P0000000-0000-0000-0000-000000000005` | Youssef Khaled Tarek | +15 minutes from seeding |

---
## 15. RefreshTokens (25 — one per user)
| User | Role | Token (prefix) |
| :--- | :--- | :--- |
| Ahmed Mahmoud El-Sayed | Patient | `RT_PATIENT_01_...` |
| Sara Hassan Ibrahim | Patient | `RT_PATIENT_02_...` |
| Mohamed Ali Abdel-Rahman | Patient | `RT_PATIENT_03_...` |
| Mona Omar Mostafa | Patient | `RT_PATIENT_04_...` |
| Youssef Khaled Tarek | Patient | `RT_PATIENT_05_...` |
| Dr. Khaled Mansour El-Din | Pharmacist | `RT_PHARMA_01_...` |
| Dr. Hoda Youssef Fathy | Pharmacist | `RT_PHARMA_02_...` |
| Dr. Amr Nabil Gamal | Pharmacist | `RT_PHARMA_03_...` |
| Dr. Rania Abdel-Aziz | Pharmacist | `RT_PHARMA_04_...` |
| Dr. Yasser Ahmed Fouad | Pharmacist | `RT_PHARMA_05_...` |
| Pharmacy Admin Ezaby | PharmacyAdmin | `RT_PADMIN_01_...` |
| Pharmacy Admin Seif | PharmacyAdmin | `RT_PADMIN_02_...` |
| Pharmacy Admin Delmar | PharmacyAdmin | `RT_PADMIN_03_...` |
| Pharmacy Admin 19011 | PharmacyAdmin | `RT_PADMIN_04_...` |
| Pharmacy Admin Misr | PharmacyAdmin | `RT_PADMIN_05_...` |
| System Admin Core | Admin | `RT_ADMIN_01_...` |
| System Admin Operations | Admin | `RT_ADMIN_02_...` |
| System Admin Audit | Admin | `RT_ADMIN_03_...` |
| System Admin Technical | Admin | `RT_ADMIN_04_...` |
| System Admin Governance | Admin | `RT_ADMIN_05_...` |
| Dr. Tarek El-Kashif | ReviewTeam | `RT_REVIEW_01_...` |
| Dr. Nouran Sherif | ReviewTeam | `RT_REVIEW_02_...` |
| Dr. Kareem Abdel-Moneim | ReviewTeam | `RT_REVIEW_03_...` |
| Dr. Salma El-Ghazaly | ReviewTeam | `RT_REVIEW_04_...` |
| Dr. Hesham El-Naggar | ReviewTeam | `RT_REVIEW_05_...` |