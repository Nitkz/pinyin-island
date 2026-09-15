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
├── assets/
│   ├── audio/
│   │   ├── initials/         # เสียงพยัญชนะเดี่ยว (b.mp3, zh.mp3, ...)
│   │   ├── finals/           # เสียงสระเดี่ยว/ผสม (a.mp3, ai.mp3, v.mp3, ...)
│   │   └── sfx/              # เสียงเอฟเฟกต์ (correct.mp3, try_again.mp3, cheer.mp3)
│   ├── clips/                # วิดีโอสั้นครูผู้สอนสำหรับแต่ละพยางค์
│   └── images/
│       ├── ui/               # speaker.svg, star.svg, island_map.svg
│       └── cards/            # การ์ดปุ่มกดลายการ์ตูน
├── src/
│   ├── components/           # UI Components (GameCanvas, AudioPlayer, Card)
│   ├── data/
│   │   └── questions.json    # คลังข้อสอบ ด่าน และลำดับเสียง
│   └── state/                # State management (Player progress & stars)
└── README.md
