Backend (One-JobOnlineAPI) เพิ่ม master data + CRUD API สำหรับ "ระดับ" (Level) และ "ประเภทพนักงาน" (EmployeeType) เสร็จแล้ว โดยเดินตามแพทเทิร์นเดียวกับ `JobGroups` ทุกอย่าง (เอกสารอ้างอิง: `frontend_jobgroups_manage_prompt.md`) พร้อมกับเปลี่ยน `Jobs.tier`/`Jobs.employeeType` จาก free-text string เดิมให้เป็น FK ผูกกับตารางใหม่นี้แทน สรุปให้ครบเพื่อผูก UI ต่อได้เลย:

## 0. สำคัญ — เวลา deploy ยังไม่ตรงกัน อย่าเพิ่งปิดสวิตช์ของเก่า

โค้ด backend (Controllers/Models/SQL) ทั้งหมดที่พูดถึงในเอกสารนี้ push ขึ้น `develop` แล้ว **แต่ SQL migration script ที่สร้างตาราง `JobLevels`/`EmployeeTypes` และเพิ่มคอลัมน์ `LevelID`/`EmployeeTypeID` ใน `Jobs` ยังไม่ได้รันขึ้น database จริง** เพราะฉะนั้นตอนนี้ endpoint ใหม่ทั้งหมดในเอกสารนี้จะยัง 500/error อยู่จนกว่าจะรัน script เสร็จ — จะแจ้งอีกทีตอน migration รันขึ้นจริงแล้ว ระหว่างนี้พัฒนา UI รอไว้ก่อนได้ (ต่อ endpoint แล้ว mock/รอ deploy) แต่ **อย่าลบ code เดิมที่ผูกกับ `job.tier`/`job.employeeType` (string) ทิ้งจนกว่าจะยืนยันว่า deploy แล้ว**

## 1. Endpoint ใหม่: `/api/JobLevels` และ `/api/EmployeeTypes` — CRUD เต็มรูปแบบ เหมือน `/api/JobGroups` ทุกกระเบียดนิ้ว

Route/behavior เหมือนกันทั้ง 2 ตัว แค่ชื่อ field เปลี่ยน:

| JobGroups (เดิม) | JobLevels (ใหม่) | EmployeeTypes (ใหม่) |
|---|---|---|
| `jobGroupID` | `jobLevelID` | `employeeTypeID` |
| `groupName` | `levelName` | `typeName` |

Field อื่น (`sortOrder`, `isActive`, `createdBy`, `createdDate`, `updatedBy`, `updatedDate`) shape เหมือนกันเป๊ะ

```
GET    /api/JobLevels?includeInactive=true|false   -> list (default เฉพาะ active)
GET    /api/JobLevels/{id}                          -> ทีละรายการ
POST   /api/JobLevels        (ต้องมี JWT Bearer)     -> สร้างใหม่ -> 201 { jobLevelID }
PUT    /api/JobLevels/{id}   (ต้องมี JWT Bearer)     -> แก้ไข -> คืน object เต็ม
DELETE /api/JobLevels/{id}?updatedBy=xxx (ต้องมี JWT Bearer) -> soft delete (ตั้ง isActive=0 ไม่ได้ลบจริง)
```

```
GET    /api/EmployeeTypes?includeInactive=true|false
GET    /api/EmployeeTypes/{id}
POST   /api/EmployeeTypes    (ต้องมี JWT Bearer)
PUT    /api/EmployeeTypes/{id} (ต้องมี JWT Bearer)
DELETE /api/EmployeeTypes/{id}?updatedBy=xxx (ต้องมี JWT Bearer)
```

Sample response `GET /api/JobLevels` (ตรงกับ list ที่ frontend เคย hardcode ไว้ก่อนหน้านี้):
```json
[
  { "jobLevelID": 1, "levelName": "Junior", "sortOrder": 1, "isActive": true, "createdBy": null, "createdDate": "2026-07-22T00:00:00", "updatedBy": null, "updatedDate": null },
  { "jobLevelID": 2, "levelName": "Mid Level", "sortOrder": 2, "isActive": true },
  { "jobLevelID": 3, "levelName": "Senior", "sortOrder": 3, "isActive": true },
  { "jobLevelID": 4, "levelName": "Manager", "sortOrder": 4, "isActive": true },
  { "jobLevelID": 5, "levelName": "Director", "sortOrder": 5, "isActive": true },
  { "jobLevelID": 6, "levelName": "Executive Director", "sortOrder": 6, "isActive": true },
  { "jobLevelID": 7, "levelName": "VP", "sortOrder": 7, "isActive": true },
  { "jobLevelID": 8, "levelName": "SVP", "sortOrder": 8, "isActive": true },
  { "jobLevelID": 9, "levelName": "EVP", "sortOrder": 9, "isActive": true },
  { "jobLevelID": 10, "levelName": "C-Level", "sortOrder": 10, "isActive": true }
]
```

Sample response `GET /api/EmployeeTypes` (seed 5 ค่า):
```json
[
  { "employeeTypeID": 1, "typeName": "พนักงานประจำ", "sortOrder": 1, "isActive": true },
  { "employeeTypeID": 2, "typeName": "พนักงานสัญญาจ้าง", "sortOrder": 2, "isActive": true },
  { "employeeTypeID": 3, "typeName": "ฟรีแลนซ์", "sortOrder": 3, "isActive": true },
  { "employeeTypeID": 4, "typeName": "พาร์ทไทม์", "sortOrder": 4, "isActive": true },
  { "employeeTypeID": 5, "typeName": "นักศึกษาฝึกงาน", "sortOrder": 5, "isActive": true }
]
```

Auth/soft-delete behavior เหมือน `JobGroups` เป๊ะ — `POST`/`PUT`/`DELETE` ทั้งหมดต้องแนบ `Authorization: Bearer <token>` ไม่งั้น `401`, และ `DELETE` เป็นแค่ตั้ง `isActive=false` (กู้คืนได้ผ่าน `PUT` ตั้ง `isActive: true` กลับ) ด้วยเหตุผลเดียวกับ JobGroups: `Jobs.LevelID`/`Jobs.EmployeeTypeID` อาจอ้างอิงแถวนี้อยู่

## 2. Endpoint dropdown แบบเบา (ไม่ต้อง auth) — สำหรับผูกฟอร์มสร้าง/แก้ไข Job

```
GET /api/Jobs/job-levels       -> [{ "id": 1, "name": "Junior" }, { "id": 2, "name": "Mid Level" }, ...]
GET /api/Jobs/employee-types   -> [{ "id": 1, "name": "พนักงานประจำ" }, { "id": 2, "name": "พนักงานสัญญาจ้าง" }, ...]
```

คืนเฉพาะ active, เรียงตาม `sortOrder` มาให้แล้ว (ไม่ต้อง sort ซ้ำฝั่ง frontend) รองรับ query param กรองทีละตัวได้เหมือนกัน (`?jobLevelId=` / `?employeeTypeId=`) แต่ปกติใช้แบบไม่ใส่ query เพื่อดึงทั้ง list มาผูก dropdown

หมายเหตุ: นี่คนละ route กับ `/api/JobLevels`/`/api/EmployeeTypes` ในข้อ 1 — สองตัวนี้ (`/api/Jobs/job-levels`, `/api/Jobs/employee-types`) ไม่ต้อง auth และ field แค่ `id`/`name` เท่านั้น (ตามแพทเทิร์นเดียวกับ `/api/Jobs/job-groups` เดิมที่มีอยู่แล้ว) ใช้สำหรับหน้าสร้าง/แก้ไข Job เท่านั้น ไม่ใช่หน้า Admin จัดการ master data (ให้ใช้ endpoint ข้อ 1 สำหรับหน้านั้น)

## 3. `Jobs` เปลี่ยนจาก free-text เป็น FK — **breaking change**

**เดิม** (ตามที่เคยแจ้งใน `frontend_job_fields_update_prompt.md`): `job.tier` (string, เช่น `"Senior"`), `job.employeeType` (string, เช่น `"Full-time"`)

**ใหม่**: field เหล่านี้ถูกลบออกจาก response ไปเลย แทนที่ด้วย:
- `job.levelID` (int, nullable) + `job.levelName` (string, nullable) — ชื่อ join มาให้แล้ว ไม่ต้อง join เอง
- `job.employeeTypeID` (int, nullable) + `job.employeeTypeName` (string, nullable)

Sample response `GET /api/Jobs/{id}` (หลัง migration รันแล้ว):
```json
{
  "jobID": 10,
  "jobTitle": "IT Administrator",
  "jobGroupID": 1,
  "office": "สำนักงานใหญ่",
  "levelID": 3,
  "levelName": "Senior",
  "employeeTypeID": 1,
  "employeeTypeName": "พนักงานประจำ"
}
```

เหมือนเดิม — backend serializer ตัด field ที่เป็น `NULL` ออกจาก JSON ไปเลย (ไม่ส่งมาเป็น `null`) เพราะฉะนั้นต้องรองรับกรณี key พวกนี้ไม่มีอยู่ใน object เลย (`job.levelName ?? "ไม่ระบุ"`) โดยเฉพาะ Job เก่าที่ backfill ไม่ตรงชื่อ (ดูข้อ 4)

**`POST /api/Jobs` และ `PUT /api/Jobs/{id}` รับ `levelID`/`employeeTypeID` เป็น int แทน `tier`/`employeeType` string** — ต้องเปลี่ยนฟอร์มสร้าง/แก้ไข Job จาก text input เป็น dropdown ที่ผูกกับ endpoint ข้อ 2 (ส่งแค่ id ที่เลือก ไม่ใช่ชื่อ)

## 4. เรื่อง backfill ข้อมูลเก่า

Migration จะพยายาม map `Jobs.Tier`/`Jobs.EmployeeType` (string เดิม) ไปเป็น `LevelID`/`EmployeeTypeID` โดย match ชื่อ exact ตรงตัว ถ้า job เก่าตัวไหนมีค่า string ที่ไม่ตรงกับชื่อใน master data ใหม่เป๊ะ (เช่นพิมพ์ต่างกัน) จะ**ไม่ auto-map ให้** ปล่อยเป็น `NULL` ไว้ — เจอ job แบบนี้ให้โชว์ fallback "ไม่ระบุ" เหมือนเดิม ไม่ต้อง error

## 5. field เดิมที่เคยผูกผิด (`frontend_job_fields_update_prompt.md` ข้อ 2) ต้องอัปเดตตาม

ถ้าหน้า Job Detail เคยผูก label "ประเภทงาน" กับ `job.employeeType` (string) ตามที่แจ้งไปรอบก่อน ตอนนี้ต้องเปลี่ยนไปผูกกับ **`job.employeeTypeName`** แทน (field เดิมไม่มีอยู่ใน response แล้ว)

---

**สิ่งที่อยากให้ทำต่อ**:
1. สร้างหน้า Admin ใหม่ 2 หน้า "จัดการระดับ" และ "จัดการประเภทพนักงาน" โครงเดียวกับหน้า "จัดการกลุ่มงาน" ที่ทำไว้แล้ว (ตาราง list + เพิ่ม/แก้ไข/ลบ/กู้คืน) ดึงจาก `GET /api/JobLevels` / `GET /api/EmployeeTypes`
2. เพิ่ม TypeScript type `JobLevel` (`jobLevelID`, `levelName`, `sortOrder`, `isActive`, `createdBy`, `createdDate`, `updatedBy`, `updatedDate`) และ `EmployeeType` (`employeeTypeID`, `typeName`, ...) ตาม shape ข้อ 1
3. แก้ type `Job` — เอา `tier`/`employeeType` (string) ออก ใส่ `levelID`, `levelName`, `employeeTypeID`, `employeeTypeName` แทน
4. แก้ฟอร์มสร้าง/แก้ไข Job — เปลี่ยน input `tier`/`employeeType` จาก text ธรรมดา เป็น dropdown ดึงจาก `GET /api/Jobs/job-levels` และ `GET /api/Jobs/employee-types` (ข้อ 2) ส่งค่าเป็น `levelID`/`employeeTypeID` (int) ตอน submit
5. แก้จุดที่เคยโชว์ `job.tier`/`job.employeeType` (list, detail, การ์ด) ให้ไปโชว์ `job.levelName`/`job.employeeTypeName` แทน พร้อม fallback "ไม่ระบุ" ถ้าไม่มีค่า
6. รอ backend ยืนยันว่า migration รันขึ้น dev DB แล้ว (ดูข้อ 0) ก่อนขึ้น production ด้วยโค้ดชุดนี้ — ถ้ายังไม่ยืนยัน ให้พัฒนาไว้ก่อนได้แต่ยังไม่ต้อง merge ทับของเดิม
7. Sort ตาราง list ทั้ง 2 หน้าตาม `sortOrder` ให้ลากจัดลำดับได้ก็ได้ (ถ้าต้องการ) เหมือนที่ทำไว้กับหน้า JobGroups
