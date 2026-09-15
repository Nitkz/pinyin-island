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
│   │   ├── PinyinItem.cs       # ข้อมูลตัวอักษร เช่น Id, Char, Type, AudioPath
│   │   ├── Question.cs         # คำถามแต่ละข้อ (ListenPick, Sequence, Match)
│   │   ├── Stage.cs            # ข้อมูลด่าน 1-14 และเกาะ 1-4
│   │   └── UserProgress.cs     # บันทึกสถานะการปลดล็อกด่านและดาว (0-3)
│   │
│   ├── Services/               # Business Logic & Browser Interop
│   │   ├── IAudioService.cs    # Interface เล่นเสียง SFX / Voice
│   │   ├── AudioService.cs     # JSInterop ควบคุม Web Audio / Howler.js
│   │   ├── IGameService.cs     # Interface ควบคุมคำถามและคำนวณดาว
│   │   ├── GameService.cs      # Game State Machine
│   │   └── ProgressService.cs  # เซฟ/โหลดสถิติลง LocalStorage
│   │
│   ├── Components/             # Reusable UI สำหรับเด็ก ป.1
│   │   ├── Common/
│   │   │   ├── SpeakerButton.razor # ปุ่มลำโพงกลมขนาดใหญ่สำหรับกดฟังซ้ำ
│   │   │   └── StarRating.razor    # แสดงผลดาว 1-3 ดาว
│   │   ├── Dialogs/
│   │   │   └── StageClearDialog.razor # MudDialog แจกดาวเมื่อจบด่าน
│   │   └── GameModes/
│   │       ├── ListenAndTap.razor  # โหมด 1: แตะการ์ดตามเสียง
│   │       ├── TrainSequence.razor # โหมด 2: ลากต่อขบวนรถไฟ
│   │       └── CardMatch.razor     # โหมด 3: จับคู่เสียง-การ์ด
│   │
│   ├── Pages/                  # หน้าจอหลักของเกม
│   │   ├── Home.razor          # Title Screen (ปุ่มเริ่มเล่นใหญ่ๆ)
│   │   ├── IslandMap.razor     # แผนที่เลือก 4 เกาะ / 14 ด่าน
│   │   └── GamePlay.razor      # หน้าเล่นเกมหลัก (ผูก Route: /play/{stageId:int})
│   │
│   └── wwwroot/                # Static Assets (เข้าถึงผ่าน /assets/...)
│       ├── assets/
│       │   ├── audio/
│       │   │   ├── initials/   # b.mp3, p.mp3, ...
│       │   │   ├── finals/     # a.mp3, ai.mp3, v.mp3, ...
│       │   │   └── sfx/        # correct.mp3, wrong.mp3, cheer.mp3
│       │   ├── video/
│       │   │   ├── initials/   # b.mp4, p.mp4, ...
│       │   │   └── finals/     # a.mp4, ai.mp4, v.mp4, ...
│       │   └── data/
│       │       └── stages.json # คลังข้อมูลด่านและคำถาม 53 ข้อ
│       └── js/
│           └── audio-player.js # สคริปต์ JS จัดการเล่นเสียงแบบ Zero-Latency
│
├── PinyinIsland.slnx
└── README.md
```
