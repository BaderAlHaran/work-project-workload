# أوراق العمل — المرحلة الثانوية

A small Arabic Windows app for distributing high-school (grades 10–12) computer worksheets to students.
The worksheets come from the Ministry of Education site: https://labonline.moe.edu.kw/WorkpaperView/Index

## Features

- **Student login:** name, grade, section (10/1–10/9, 11/1–11/9, 12/1–12/9), and علمي / أدبي track for grades 11 and 12.
- **Worksheet cards:** opening one copies it to
  `Desktop\ملفات الطلاب\<الصف>\<الشعبة>\<الاسم>\ورقة عمل N - …` and opens it. Existing student work is never overwritten.
- **Admin panel** (default password `1234`): add, rename and delete worksheets (drag and drop works), open student folders and the login log, change the password, create shortcuts.
- **Shared network drive:** run it from a mapped drive (e.g. `M:`) and every PC sees the admin's changes. Open student windows refresh within about 20 seconds.
- **Self-repairing shortcuts:** Desktop and Start menu shortcuts are re-pointed to the running exe on every start.

## Folder layout

```
build/
├── أوراق العمل.exe        the app (~50 KB, needs only the .NET Framework built into Windows)
├── المكتبة/               worksheet library
│   ├── الصف العاشر/
│   ├── الصف الحادي عشر/
│   │   ├── علمي/          shown only to the science track
│   │   └── أدبي/          shown only to the literature track
│   └── الصف الثاني عشر/   (same علمي / أدبي subfolders)
├── settings.ini           admin password hash (created on first change)
└── سجل الدخول.csv         login log (created at runtime)
```

Anything placed directly in a grade folder is shared by every track in that grade.

## Build

Uses the C# compiler that ships with Windows, so no SDK is needed:

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Source is in `src/` (C# 5, WinForms, .NET Framework 4.x).

## Deploy

Copy the whole `build` folder to the shared drive, e.g. `M:\أوراق العمل\`, and open the exe once on each PC to create its shortcut.
