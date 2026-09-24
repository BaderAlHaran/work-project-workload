import sqlite3

# ==========================================
# 1. تهيئة قاعدة بيانات الكتب (grade11_books.db)
# ==========================================
print("جاري العمل على قاعدة بيانات grade11_books...")
conn1 = sqlite3.connect(r'..\grade11_books.db')
cursor1 = conn1.cursor()

# حذف الجداول للبدء من جديد
cursor1.execute('DROP TABLE IF EXISTS books')
cursor1.execute('DROP TABLE IF EXISTS publishers')

# إنشاء جدول الناشرين
cursor1.execute('''
CREATE TABLE publishers (
    id INTEGER PRIMARY KEY,
    name TEXT,
    email TEXT
)
''')

# إنشاء جدول الكتب مع إضافة حقل cover في النهاية بنوع BLOB
cursor1.execute('''
CREATE TABLE books (
    id INTEGER PRIMARY KEY,
    title TEXT NOT NULL,
    author TEXT,
    category TEXT,
    year INTEGER,
    price REAL,
    cover BLOB
)
''')

# بيانات الناشرين
publishers_data = [
    ('Arabic Supervision Department', 'arabic@moe.edu.kw'),
    ('Islamic Supervision Department', 'islamic@moe.edu.kw'),
    ('French Supervision Department', 'french@moe.edu.kw'),
    ('english Supervision Department', 'english@moe.edu.kw'),
    ('Mathematics Supervision Department', 'maths@moe.edu.kw'),
    ('Science Supervision Department', 'science@moe.edu.kw'),
    ('ICT Supervision Department', 'ict@moe.edu.kw'),
    ('Social Supervision Department', 'social@moe.edu.kw')
]
cursor1.executemany('INSERT INTO publishers (name, email) VALUES (?, ?)', publishers_data)

# بيانات الكتب (تم ترك حقل cover فارغاً حالياً)
books_data = [
    ('Arabic', 'Arabic SD', 'Art/Science', 2001, 1.5),
    ('French', 'French SD', 'Art', 2007, 1.5),
    ('English', 'English SD', 'Art/Science', 2009, 1.5),
    ('Mathematics', 'Mathematics SD', 'Science', 2013, 1.5),
    ('Mathematics', 'Mathematics SD', 'Art', 2013, 1.5),
    ('Principles of Geography and Economics', 'Social SD', 'Art', 2016, 1.5),
    ('Psychology and sociology', 'Social SD', 'Art', 2016, 1.5),
    ('Islamic History', 'Social SD', 'Art', 2007, 1.5),
    ('Chemistry', 'Science SD', 'Science', 2013, 1.5),
    ('Physics', 'Science SD', 'Science', 2013, None),
    ('Biology', 'Science SD', 'Science', 2013, 1.5),
    ('Geology', 'Science SD', 'Science', 2013, 1.5),
    ('ICT', 'ICT SD', 'Art/Science', 2025, 1.5),
    ('Quran', 'Islamic SD', 'Art/Science', 2003, 1.5),
    ('Islamic Studies', 'Islamic SD', 'Art/Science', 2013, 1.5)
]
# نحدد الأعمدة في الإدخال لأننا لم ندرج الـ id ولا الـ cover في مصفوفة البيانات
cursor1.executemany('INSERT INTO books (title, author, category, year, price) VALUES (?, ?, ?, ?, ?)', books_data)

conn1.commit()
conn1.close()
print("تم الانتهاء من قاعدة بيانات الكتب.")

print("-" * 30)

# ==========================================
# 2. تهيئة قاعدة بيانات المعالم (Kuwait_landmarks.db)
# ==========================================
print("جاري العمل على قاعدة بيانات Kuwait_landmarks...")
conn2 = sqlite3.connect(r'..\Kuwait_landmarks.db')
cursor2 = conn2.cursor()

# حذف الجداول
cursor2.execute('DROP TABLE IF EXISTS landmarks')
cursor2.execute('DROP TABLE IF EXISTS events')

# إنشاء جدول المعالم
cursor2.execute('''
CREATE TABLE landmarks (
    id INTEGER PRIMARY KEY,
    name TEXT,
    location TEXT,
    category TEXT,
    year INTEGER,
    price REAL,
    info TEXT
)
''')

# إنشاء جدول الفعاليات
cursor2.execute('''
CREATE TABLE events (
    id INTEGER PRIMARY KEY,
    landmark_id INTEGER,
    event_name TEXT,
    event_date TEXT,
    description TEXT,
    FOREIGN KEY(landmark_id) REFERENCES landmarks(id)
)
''')

# بيانات المعالم
landmarks_data = [
    (1, 'Al-Tahrir Tower', 'Central Kuwait City', 'Architecture', 1996, 0.0, 'Al-Tahrir Tower is one of the most iconic landmarks in Kuwait...'),
    (2, 'Kuwait National Museum', 'Al-Murqab Area', 'Historical', 1983, 2.5, 'The museum houses cultural and historical artifacts...'),
    (3, 'The Avenues Mall', 'Al-Rai Area', 'Shopping', 2007, 0.0, 'One of the largest and most popular shopping malls...'),
    (4, 'Failaka Island', 'Off the eastern coast of Kuwait', 'Historical', 1950, 5.0, 'An island with archaeological sites...'),
    (5, 'Souq Al-Mubarakiya', 'Central Kuwait City', 'Traditional', 1897, 0.0, 'A traditional market offering a glimpse of Kuwait’s heritage...'),
    (6, 'Sheikh Jaber Al-Ahmad Bridge', 'Connecting Kuwait City to the south', 'Modern Infrastructure', 2019, 0.0, 'A remarkable modern bridge...'),
    (7, 'Kuwait National Library', 'Al-Murqab Area', 'Cultural', 1994, 0.0, 'A prominent library housing an extensive collection...'),
    (8, 'Museum of Modern Art', 'Salmiya Area', 'Art', 1980, 1.0, 'Home to contemporary and modern art collections.'),
    (9, 'Sheikh Jaber Al-Ahmad Cultural Center', 'Kuwait City', 'Cultural', 2016, 0.0, 'Largest cultural center and opera house in the Middle East.'),
    (10, 'Grand Mosque of Kuwait', 'Kuwait City', 'Religious Architecture', 1986, 0.0, 'The Grand Mosque is the largest mosque in Kuwait.')
]

# بيانات الفعاليات
events_data = [
    (1, 1, 'Modern Art Workshop', '30-06-2025', 'Interactive workshop for young artist'),
    (2, 1, 'Traditional Market Week', '02-08-2025', 'Cultral market event with crafts and food stalls.'),
    (3, 5, 'National Food Festival', '02-08-2025', 'A vibrant festival showcasing traditional Kuwaiti cuisine.'),
    (4, 3, 'Theater Night', '20-07-2025', 'Stage performance at the cultural center.'),
    (5, 4, 'History of Failaka Talk', '09-09-2025', 'Lecture about the archaeological sites.')
]

cursor2.executemany('INSERT INTO landmarks VALUES (?,?,?,?,?,?,?)', landmarks_data)
cursor2.executemany('INSERT INTO events VALUES (?,?,?,?,?)', events_data)

conn2.commit()
conn2.close()
print("تم الانتهاء من قاعدة بيانات المعالم.")

print("\nاكتملت جميع العمليات بنجاح!")