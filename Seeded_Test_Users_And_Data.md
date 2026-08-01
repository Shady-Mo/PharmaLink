# PharmaLink Complete Seeded Test Data Reference

This document provides a comprehensive reference of **all 17 database tables** populated by the T-SQL seeding script.

> [!IMPORTANT]
> All 20 created users share the same default password for local development and API testing.  
> **Default Password:** `P@ss1234`

---

## Table of Contents
1. [AspNetUsers (20 Users)](#1-aspnetusers-20-users)
2. [AspNetUserRoles](#2-aspnetuserroles)
3. [Pharmacies](#3-pharmacies)
4. [PharmacyBranches](#4-pharmacybranches)
5. [Addresses](#5-addresses)
6. [PharmacistAssignments](#6-pharmacistassignments)
7. [PharmacyInventories](#7-pharmacyinventories)
8. [Carts](#8-carts)
9. [CartItems](#9-cartitems)
10. [Orders](#10-orders)
11. [OrderItems](#11-orderitems)
12. [OrderFulfillmentLegs](#12-orderfulfillmentlegs)
13. [OrderFulfillmentLegStatusAudits](#13-orderfulfillmentlegstatusaudits)
14. [PrescriptionReviews](#14-prescriptionreviews)
15. [PrescriptionReviewMedicines](#15-prescriptionreviewmedicines)
16. [PhoneVerificationOtps](#16-phoneverificationotps)
17. [RefreshTokens](#17-refreshtokens)

---

## 1. AspNetUsers (20 Users)

### 👤 Patients (5 Users)
| Full Name | Email / Username | Password | Phone Number | UserType | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** | `patient1@ph-link.com` | `P@ss1234` | `+201011111101` | Patient | Active (1) |
| **Sara Hassan** | `patient2@ph-link.com` | `P@ss1234` | `+201011111102` | Patient | Active (1) |
| **Mohamed Ibrahim** | `patient3@ph-link.com` | `P@ss1234` | `+201011111103` | Patient | Active (1) |
| **Mona Ali** | `patient4@ph-link.com` | `P@ss1234` | `+201011111104` | Patient | Active (1) |
| **Omar Tarek** | `patient5@ph-link.com` | `P@ss1234` | `+201011111105` | Patient | Active (1) |

### 👨‍⚕️ Pharmacists (1 Users)
| Full Name | Email / Username | Password | Phone Number | UserType | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Dr. Omar Hany** | `pharmacist4@ph-link.com` | `P@ss1234` | `+201022222204` | Pharmacist | Active (1) |

### 🏥 Pharmacy Admins (5 Users)
| Full Name | Email / Username | Password | Phone Number | UserType | Managed Pharmacy | SuperAdmin |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Pharmacy Admin Ezaby** | `pharmadmin1@ph-link.com` | `P@ss1234` | `+201044444401` | PharmacyAdmin | El-Ezaby Pharmacy Chain | Yes (1) |
| **Pharmacy Admin Seif** | `pharmadmin2@ph-link.com` | `P@ss1234` | `+201044444402` | PharmacyAdmin | Seif Pharmacies Group | No (0) |
| **Pharmacy Admin Delmar** | `pharmadmin3@ph-link.com` | `P@ss1234` | `+201044444403` | PharmacyAdmin | Delmar & Attalla | No (0) |
| **Pharmacy Admin 19011** | `pharmadmin4@ph-link.com` | `P@ss1234` | `+201044444404` | PharmacyAdmin | 19011 Pharmacies | No (0) |
| **Pharmacy Admin Misr** | `pharmadmin5@ph-link.com` | `P@ss1234` | `+201044444405` | PharmacyAdmin | Misr Pharmacies | No (0) |

### ⚙️ System Admins (5 Users)
| Full Name | Email / Username | Password | Phone Number | UserType | Scope |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **System Admin One** | `sysadmin1@ph-link.com` | `P@ss1234` | `+201033333301` | SystemAdmin | Platform Scope |
| **System Admin Two** | `sysadmin2@ph-link.com` | `P@ss1234` | `+201033333302` | SystemAdmin | Platform Scope |
| **System Admin Three** | `sysadmin3@ph-link.com` | `P@ss1234` | `+201033333303` | SystemAdmin | Platform Scope |
| **System Admin Four** | `sysadmin4@ph-link.com` | `P@ss1234` | `+201033333304` | SystemAdmin | Platform Scope |
| **System Admin Five** | `sysadmin5@ph-link.com` | `P@ss1234` | `+201033333305` | SystemAdmin | Platform Scope |

---

## 2. AspNetUserRoles

| User Group | Role Assigned |
| :--- | :--- |
| `patient1@ph-link.com` ... `patient5@ph-link.com` | **Patient** |
| `pharmacist1@ph-link.com` ... `pharmacist5@ph-link.com` | **Pharmacist** |
| `pharmadmin1@ph-link.com` ... `pharmadmin5@ph-link.com` | **PharmacyAdmin** |
| `sysadmin1@ph-link.com` ... `sysadmin5@ph-link.com` | **Admin** |

---

## 3. Pharmacies

| Legal Name | License Number | Logo URL | Verification Status |
| :--- | :--- | :--- | :--- |
| **El-Ezaby Pharmacy Chain** | LIC-10001 | `https://img.ph-link.com/elezaby.png` | Verified (2) |
| **Seif Pharmacies Group** | LIC-10002 | `https://img.ph-link.com/seif.png` | Verified (2) |
| **Delmar & Attalla** | LIC-10003 | `https://img.ph-link.com/delmar.png` | Verified (2) |
| **19011 Pharmacies** | LIC-10004 | `https://img.ph-link.com/19011.png` | Verified (2) |
| **Misr Pharmacies** | LIC-10005 | `https://img.ph-link.com/misr.png` | Pending (1) |

---

## 4. PharmacyBranches

| Branch Name | Pharmacy | Location | Phone Number | Working Hours | Delivery | Pickup |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **El-Ezaby Dokki Branch** | El-Ezaby | Giza, Giza | +20233344401 | 24/7 | Yes | Yes |
| **Seif Nasr City Branch** | Seif | Cairo, Cairo | +2022277702 | 08:00 AM - 12:00 AM | Yes | Yes |
| **Delmar Alexandria Branch** | Delmar | Alexandria, Alex | +2034888803 | 24/7 | Yes | Yes |
| **19011 Mansoura Branch** | 19011 | Mansoura, Dakahlia | +2050225504 | 09:00 AM - 11:00 PM | No | Yes |
| **Misr Tanta Branch** | Misr | Tanta, Gharbia | +2040331105 | 24/7 | Yes | No |

---

## 5. Addresses

| Patient Owner | Address Line | City | Governorate | Is Default |
| :--- | :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | 15 El-Tahrir Street, Dokki | Giza | Giza | Yes (1) |
| **Sara Hassan** (`patient2`) | 22 Makram Ebeid Street, Nasr City | Cairo | Cairo | Yes (1) |
| **Mohamed Ibrahim** (`patient3`) | 8 Fouad Street, Al-Attarin | Alexandria | Alexandria | Yes (1) |
| **Mona Ali** (`patient4`) | 45 El-Gomhouria Street | Mansoura | Dakahlia | Yes (1) |
| **Omar Tarek** (`patient5`) | 12 El-Gaish Street | Tanta | Gharbia | Yes (1) |

---

## 6. PharmacistAssignments

| Pharmacist | Pharmacy | Assigned By Admin | Is Active |
| :--- | :--- | :--- | :--- |
| **Dr. Khaled Mansour** | El-Ezaby Pharmacy Chain | Pharmacy Admin Ezaby | Active (1) |
| **Dr. Hoda Youssef** | Seif Pharmacies Group | Pharmacy Admin Seif | Active (1) |
| **Dr. Amr Nabil** | Delmar & Attalla | Pharmacy Admin Delmar | Active (1) |
| **Dr. Rania Gamal** | 19011 Pharmacies | Pharmacy Admin 19011 | Active (1) |
| **Dr. Yasser Fathy** | Misr Pharmacies | Pharmacy Admin Misr | Active (1) |

---

## 7. PharmacyInventories

| Branch | Stock Quantity | Reserved Quantity | Unit Price | Expiry Date |
| :--- | :--- | :--- | :--- | :--- |
| **El-Ezaby Dokki Branch** | 150 | 5 | 45.50 EGP | 2027-12-31 |
| **El-Ezaby Dokki Branch** | 80 | 0 | 120.00 EGP | 2026-10-15 |
| **Seif Nasr City Branch** | 200 | 10 | 45.50 EGP | 2028-05-20 |
| **Delmar Alexandria Branch** | 50 | 2 | 115.00 EGP | 2027-01-30 |
| **19011 Mansoura Branch** | 90 | 0 | 46.00 EGP | 2027-08-14 |

---

## 8. Carts

| Patient Owner | Created At | Updated At |
| :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | GETUTCDATE() | GETUTCDATE() |
| **Sara Hassan** (`patient2`) | GETUTCDATE() | GETUTCDATE() |

---

## 9. CartItems

| Patient Cart Owner | Quantity | Unit Price Snapshot |
| :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | 2 | 45.50 EGP |
| **Ahmed Mahmoud** (`patient1`) | 1 | 120.00 EGP |
| **Sara Hassan** (`patient2`) | 3 | 45.50 EGP |

---

## 10. Orders

| Patient Owner | Delivery Address | Fulfillment Mode | Order Status | Total Amount |
| :--- | :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | Dokki, Giza | Delivery (1) | Delivered (4) | 211.00 EGP |
| **Sara Hassan** (`patient2`) | Nasr City, Cairo | Delivery (1) | Processing (2) | 45.50 EGP |

---

## 11. OrderItems

| Order Owner | Supplying Branch | Quantity Needed | Item Status |
| :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | El-Ezaby Dokki Branch | 2 | Fulfilled (2) |
| **Ahmed Mahmoud** (`patient1`) | El-Ezaby Dokki Branch | 1 | Fulfilled (2) |
| **Sara Hassan** (`patient2`) | Seif Nasr City Branch | 1 | Pending (1) |

---

## 12. OrderFulfillmentLegs

| Order Owner | Branch | Leg Type | Leg Status | Ready By Estimate | Completed At |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | El-Ezaby Dokki Branch | Preparation (1) | Completed (3) | Today | Today |
| **Sara Hassan** (`patient2`) | Seif Nasr City Branch | Preparation (1) | InProgress (2) | +2 Hours | NULL |

---

## 13. OrderFulfillmentLegStatusAudits

| Leg Target | Old Status | New Status | Reason | Changed By User |
| :--- | :--- | :--- | :--- | :--- |
| **Leg 1 (Order 1)** | Pending (1) | InProgress (2) | Started preparation | Dr. Khaled Mansour (`pharmacist1`) |
| **Leg 1 (Order 1)** | InProgress (2) | Completed (3) | Preparation completed | Dr. Khaled Mansour (`pharmacist1`) |
| **Leg 2 (Order 2)** | Pending (1) | InProgress (2) | Started preparation | Dr. Hoda Youssef (`pharmacist2`) |

---

## 14. PrescriptionReviews

| Patient Owner | Pharmacist Reviewer | Linked Order | Image Path | AI Model | Status | Review Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** | Dr. Khaled Mansour | Order #1 | `/uploads/prescriptions/rx_patient1.jpg` | gemini-1.5-flash | Verified (2) | Prescription verified successfully. |

---

## 15. PrescriptionReviewMedicines

| Prescription Review | Medicine Name | Generic Name | Dose | Dosage Form | Frequency | Duration | Confidence |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Review #1** | Panadol Extra | Paracetamol / Caffeine | 500mg/65mg | Tablet | Every 8 hours | 5 days | 98% |
| **Review #1** | Augmentin | Amoxicillin / Clavulanic Acid | 1g | Tablet | Every 12 hours | 7 days | 95% |

---

## 16. PhoneVerificationOtps

| Target User | Attempt Count | Expires At |
| :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | 1 | +10 Minutes |
| **Sara Hassan** (`patient2`) | 0 | +10 Minutes |

---

## 17. RefreshTokens

| Target User | Token | Expires On | Status |
| :--- | :--- | :--- | :--- |
| **Ahmed Mahmoud** (`patient1`) | `refresh_token_patient1_dummy_string_xyz` | +7 Days | Active |
| **Dr. Khaled Mansour** (`pharmacist1`) | `refresh_token_pharmacist1_dummy_string_xyz` | +7 Days | Active |
| **Pharmacy Admin Ezaby** (`pharmadmin1`) | `refresh_token_pharmadmin1_dummy_string_xyz` | +7 Days | Active |
