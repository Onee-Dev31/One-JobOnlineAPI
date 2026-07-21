Backend (One-JobOnlineAPI) เพิ่ม endpoint ใหม่สำหรับค้นหาผู้สมัครด้วยชื่อ-นามสกุล แล้วดึงรายการตำแหน่งที่ผู้สมัครคนนั้นสมัครไว้ทั้งหมด เป้าหมายคือใช้ในหน้าใหม่ **Reject ตำแหน่งด้วยมือ (Manual Reject)** สำหรับ HR ที่ต้องการปฏิเสธผู้สมัครออกจากตำแหน่งใดตำแหน่งหนึ่งเป็นรายกรณี (เนื่องจาก concept auto-reject ตำแหน่งอื่นอัตโนมัติยังไม่สรุป จึงปิดไว้ก่อนใน [[sp_UpdateApplicantStatusV3]])

## Endpoint 1 — ค้นหาผู้สมัคร

```
GET /api/ApplicantNew/searchByName?name={string}
```

ต้องแนบ auth เหมือน endpoint อื่นๆ ใน `ApplicantNew` (Bearer token / cookie เดียวกับที่ใช้เรียก endpoint admin อื่นอยู่แล้ว) ถ้าไม่ผ่านจะได้ `401`

### Query parameters
- `name` — **required**, ค้นหาแบบ partial match, case-insensitive จากชื่อ-นามสกุลทั้งภาษาไทยและอังกฤษ (`FirstNameThai`/`LastNameThai`/`FirstNameEng`/`LastNameEng`) — ถ้าไม่ส่งหรือส่งค่าว่างจะได้ `400`

### Response (200)
คืนมาเป็น **flat list** หนึ่งแถวต่อหนึ่งใบสมัคร (ผู้สมัครที่สมัครหลายตำแหน่งจะมีหลายแถว ซ้ำข้อมูลผู้สมัคร) — ฝั่ง frontend ต้อง group ด้วย `applicantID` เอง

```json
[
  {
    "applicantID": 123,
    "title": "นาย",
    "firstNameThai": "สมชาย",
    "lastNameThai": "ใจดี",
    "firstNameEng": "Somchai",
    "lastNameEng": "Jaidee",
    "email": "somchai@example.com",
    "mobilePhone": "0812345678",

    "applicationID": 456,
    "jobID": 10,
    "status": "Waiting HR Nagotiate",
    "remark": null,
    "rankOfSelect": 1,
    "submissionDate": "2026-06-01T09:00:00",

    "jobTitle": "Software Engineer",
    "departmentCode": "10806",
    "departmentName": "IT Department",
    "location": "สำนักงานใหญ่"
  }
]
```

ถ้าไม่เจอผู้สมัครที่ตรงเงื่อนไข คืน `[]` (ไม่ใช่ 404)

### Error responses
- `400` — ไม่ได้ส่ง `name` หรือส่งค่าว่าง (body เป็น string message ตรงๆ)
- `401` — ไม่ได้แนบ token หรือ token หมดอายุ
- `500` — server error

---

## Endpoint 2 — Reject รายตำแหน่ง (ใช้ endpoint ที่มีอยู่แล้ว ไม่ต้องสร้างใหม่)

```
PUT /api/ApplicantNew/updateApplicantStatus
```

body ขั้นต่ำสำหรับ reject หนึ่งตำแหน่ง:
```json
{
  "ApplicantID": 123,
  "JobID": 10,
  "ApplicationID": 456,
  "Status": "Reject"
}
```

**หมายเหตุสำคัญ**: endpoint นี้จะส่งอีเมลแจ้งเฉพาะเมื่อ `TypeMail` เป็นค่าพิเศษ (`Hire`, `Selected`+batch, `Employment confirm`, `Nagotiate Process`, `notiMail`) — ถ้าไม่ส่ง `TypeMail` มา (เหมือนตัวอย่างข้างบน) จะ **ไม่มีอีเมลส่งออกไป** ตอน reject ด้วยหน้านี้ ถ้าอยากให้แจ้งผู้สมัครด้วยตอน reject ต้องคุยเพิ่มว่าจะใช้ mail type ไหน — ยังไม่ได้ตกลงกันตอนนี้ ปล่อยเป็น silent reject ไปก่อน

การ reject นี้จะอัปเดตแค่แถว `(ApplicantID, JobID)` นั้นแถวเดียวเท่านั้น ไม่กระทบตำแหน่งอื่นของผู้สมัครคนเดียวกัน (auto-reject ตำแหน่งอื่นถูกปิดไว้แล้วตามที่คุยกัน)

---

## สิ่งที่อยากให้ทำ (หน้า Manual Reject)

1. **ช่องค้นหา + ปุ่ม Search**: พิมพ์ชื่อ-นามสกุล (หรือบางส่วน) แล้วกด Search ค่อยยิง `GET /searchByName` — ไม่ใช่ autocomplete/debounce
2. **Step 1 — เลือกผู้สมัคร**: group ผลลัพธ์ด้วย `applicantID` แสดงเป็น list ให้เลือก (ชื่อเต็ม, เบอร์โทร/อีเมลไว้ช่วยแยกกรณีชื่อซ้ำ) ถ้า match ได้คนเดียวจะข้ามไป step 2 เลยก็ได้
3. **Step 2 — แสดงรายการตำแหน่งที่สมัคร**: แสดงทุกแถวของผู้สมัครที่เลือก แต่ละแถวแสดง: `jobTitle`, `departmentName` (fallback `departmentCode` ถ้า `departmentName` เป็น null), `submissionDate` (วันที่สมัคร), `status` ปัจจุบัน, `remark`
4. **เงื่อนไขปุ่ม Reject ต่อแถว**:
   - ถ้า `status === "Employment confirm"` → **แสดงสถานะเฉยๆ ไม่มีปุ่ม Reject** (เพราะตกลงเริ่มงานแล้ว ไม่ควรให้ reject ผ่านหน้านี้)
   - ถ้า `status` เป็นค่าอื่น (เช่น `Waiting HR Nagotiate`, `Nagotiate Success`, `New Candidate` ฯลฯ) → แสดงปุ่ม **Reject** ให้กดได้
5. กดปุ่ม Reject แล้วควรมี confirm dialog ก่อนยิง (เพราะ action ทำให้ตำแหน่งนั้นถูกปิดถาวร ย้อนกลับต้องแก้มือ) แล้วเรียก `PUT /updateApplicantStatus` ตาม body ด้านบน
6. Reject สำเร็จแล้ว refresh แถวนั้นเป็น `status: "Reject"` ทันที (optimistic update หรือ re-fetch ก็ได้)
7. Handle loading/empty state (ค้นหาไม่เจอ), error state (API 500), และ validation state (ค้นหาโดยไม่พิมพ์ชื่อ — กันไม่ให้ยิง request เปล่า)
