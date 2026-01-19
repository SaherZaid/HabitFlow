# HabitFlow

![.NET](https://img.shields.io/badge/.NET-9.0-purple?style=flat-square)
![MAUI](https://img.shields.io/badge/.NET%20MAUI-Cross--Platform-blueviolet?style=flat-square)
![MVVM](https://img.shields.io/badge/Architecture-MVVM-brightgreen?style=flat-square)
![LocalStorage](https://img.shields.io/badge/Storage-Preferences-orange?style=flat-square)
![Notifications](https://img.shields.io/badge/Reminders-Local%20Notifications-ff69b4?style=flat-square)

---

HabitFlow is a lightweight habit tracker built with **.NET MAUI** that helps users stay consistent, track streaks, review history, and build better daily routines.  
It’s designed to be simple, fast, and practical — but with advanced features that make it feel like a real product.

---

## 🎯 Purpose & Concept

The goal of HabitFlow is to make habit tracking **easy enough to use every day** while still offering powerful insights.

HabitFlow helps users:

- Create and manage daily habits  
- Mark habits as done and track progress  
- Build streaks and stay motivated  
- Review previous days (history + calendar)  
- Schedule daily reminders to keep consistency  

It’s the kind of small tool you open for 30 seconds… but it changes your week.

---

## 🧩 Key Features

✔ Add / delete habits (with duplicate prevention)  
✔ Daily progress (count + progress bar)  
✔ Reset today (clears checkmarks correctly)  
✔ Streaks per habit (current streak)  
✔ Best streak per habit (personal record)  
✔ History view with filters and date ranges  
✔ Calendar grid (full month view)  
✔ Day Details page (done/not done for that date)  
✔ Smart daily reminders (enable/disable + time picker)  
✔ “Saved ✅” feedback message when settings are saved  
✔ Basic weekly insights + report exporting (PDF text report)

---

## 📱 Screens & Flow

- **Today** → check habits + view progress  
- **History** → browse previous days (filter + timeline)  
- **Calendar** → month grid with completion summary  
- **Day Details** → open any date and see what was done  
- **Stats** → best habit + achievements / streak highlights  
- **Reminders** → schedule daily notification and confirm save  

---

## 🛠️ Tech Stack

- .NET 9
- .NET MAUI (Cross-platform UI)
- MVVM pattern
- ObservableCollection + INotifyPropertyChanged
- Microsoft.Maui.Storage.Preferences (local persistence)
- Plugin.LocalNotification (daily reminders)
- Simple PDF export (no extra PDF libraries)

---

## 💡 Development Journey

HabitFlow was built as a practical “finishable” product:  
fast iterations, real UX problems, and real fixes.

During development, key challenges included:

- Persistence logic (habits stay saved; daily checks are tracked by date)
- Streak calculation and best-streak tracking
- Calendar grid generation (42 cells, Monday-first)
- Windows stability fixes (UI thread updates for collections)
- Notifications scheduling + UX feedback for saving

This project started simple, then evolved into something much more polished and fun.

---

## 🚀 Getting Started

1) Clone the repo  
2) Open in Visual Studio 2022  
3) Restore NuGet packages  
4) Run on:
- Android Emulator / Device  
- Windows (WinUI)  

> Notifications depend on system settings and permissions (especially on Windows).

---

## 🤝 Contact

If you'd like to see more, collaborate, or contribute ideas, you're welcome to reach out.

📧 Email: Saherzaid1997@gmail.com  
🔗 LinkedIn: **https://www.linkedin.com/in/saher-zaid-4584842a7/**  
📞 Phone: +46 738 785 036

---
