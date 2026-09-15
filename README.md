# 🏝️ Pinyin Island (เกาะมหาสมบัติพินอิน)

เว็บแอปพลิเคชันเกมการศึกษาแบบโต้ตอบ (Interactive Learning Web App) เพื่อปูพื้นฐานการออกเสียงพยัญชนะ สระเดี่ยว และสระผสมภาษาจีน (Hanyu Pinyin) สำหรับเด็กชั้นประถมศึกษาปีที่ 1 เน้นการเรียนรู้ผ่านเสียง (Audio-driven) และภาพเคลื่อนไหวโดยลดภาระการอ่านตัวหนังสือภาษาไทย

---

## ✨ Key Features

* **Audio-First Gameplay:** ออกแบบให้เด็ก ป.1 จิ้มฟังเสียงพูดสำเนียงมาตรฐานผ่านไอคอนลำโพงขนาดใหญ่ ไม่ต้องอ่านโจทย์ยาว
* **4 Adventure Islands & 14 Stages:** ผจญภัยผ่าน 4 แดนดิน สะสมดาว (1-3 ดาว) เพื่อปลดล็อกด่าน
  * **Island 1:** ดินแดนพยัญชนะตัวแรก (b, p, m, f, d, t, n, l, g, k, h)
  * **Island 2:** ดินแดนพยัญชนะท้าทาย (j, q, x, zh, ch, sh, r, z, c, s, y, w)
  * **Island 3:** หุบเขาสระเดี่ยว (a, o, e, i, u, ü)
  * **Island 4:** ถ้ำมหาสมบัติสระผสม (Compound & Nasal Finals)
* **3 Engaging Game Modes:**
  * **Listen & Tap:** ฟังเสียงครูแล้วแตะป้ายพินอินให้ถูกต้อง
  * **Pinyin Train (Sequence):** ลากตัวอักษรต่อโบกี้รถไฟเรียงลำดับหมวดหมู่
  * **Treasure Match:** เปิดหีบสมบัติจับคู่เสียงและรูปตัวพินอิน
* **Instant Feedback & Gamification:** เอฟเฟกต์พลุกระดาษและเสียงเชียร์เมื่อตอบถูก ไม่มี Game Over เพื่อส่งเสริมการเรียนรู้เชิงบวก

---

## 📁 Project Structure

```text
pinyin-island/
│
├── PinyinIsland/               # Server / Host Project (Blazor Web App)
│   ├── Components/             # App.razor, Routes.razor, Layout
│   ├── Program.cs
│   └── wwwroot/
│
├── PinyinIsland.Client/        # Client Project (WebAssembly)
│   ├── Models/                 # Data Models & DTOs
│   │   ├── IslandModels.cs     # ข้อมูลเกาะ 1-4 และด่าน 1-14 (IslandInfo, StageInfo)
│   │   ├── StageDataModels.cs  # คลังคำถาม (ListenPick, Sequence, MatchPairs)
│   │   └── UserProgress.cs     # บันทึกสถานะปลดล็อกด่านและดาว (0-3)
│   │
│   ├── Services/               # Business Logic & Data Access
│   │   ├── IProgressService.cs # Interface เซฟ/โหลดสถิติลง LocalStorage
│   │   ├── ProgressService.cs  # จัดการคะแนน ดาว และการปลดล็อกด่าน
│   │   ├── IStageDataService.cs# Interface โหลดคลังคำถาม stages.json
│   │   └── StageDataService.cs # โหลดและแคชข้อมูล stages.json
│   │
│   ├── Components/             # Reusable UI สำหรับเด็ก ป.1
│   │   ├── StageClearDialog.razor # หน้าต่างแจกดาวและสรุปผลเมื่อจบด่าน
│   │   └── GameModes/          # โหมดเกมหลัก 3 โหมด (แยกเป็น Sub-components)
│   │       ├── ListenAndTap.razor  # โหมด 1: แตะการ์ดพาสเทลตามเสียง
│   │       ├── TrainSequence.razor # โหมด 2: ลากต่อขบวนรถไฟพินอิน
│   │       └── CardMatch.razor     # โหมด 3: เปิดการ์ด 3D จับคู่เสียง-รูป
│   │
│   ├── Pages/                  # หน้าจอหลักของเกม
│   │   ├── Home.razor          # Title Screen (ปุ่มเริ่มเล่นใหญ่ๆ)
│   │   ├── IslandMap.razor     # แผนที่เลือก 4 เกาะ / 14 ด่าน
│   │   ├── GamePlay.razor      # Shell หน้าเล่นเกมหลัก (ผูก Route: /play/{StageId:int})
│   │   └── GamePlay.razor.cs   # Code-behind จัดการ Game State & Lifecycle
│   │
│   └── wwwroot/                # Static Assets (เข้าถึงผ่าน /assets/...)
│       ├── assets/
│       │   ├── audio/
│       │   │   ├── initials/   # b.mp3, p.mp3, ... (23 ตัว)
│       │   │   ├── finals/     # a.mp3, ai.mp3, vn.mp3, ... (24 ตัว)
│       │   │   └── bgm-map.mp3 # เพลงประกอบแผนที่
│       │   ├── videos/
│       │   │   ├── initials/   # b.mp4, p.mp4, ... (23 คลิป)
│       │   │   ├── finals/     # a.mp4, ai.mp4, vn.mp4, ... (24 คลิป)
│       │   │   └── panda-waving.mp4
│       │   ├── images/         # train-engine, train-carriage, card-back, block-base, ...
│       │   └── data/
│       │       └── stages.json # คลังข้อมูลด่านและคำถาม 65 ข้อ (3 โหมด)
│       └── js/
│           └── gameAudio.js    # Web Audio API Zero-Latency SFX & Video Controller
│
├── PinyinIsland.slnx
└── README.md
```
